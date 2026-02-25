using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Component;
using PurchaseWeb.Services;
using System.Text.Json;

namespace PurchaseWeb.Pages;

// QRコードのデータ構造
public class QrPayload
{
    public string TenantId { get; set; } = string.Empty;
    public List<QrPayloadItem> Items { get; set; } = [];
}

public class QrPayloadItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public partial class Register : IDisposable
{
    public required ApplicationDbContext DbFactory { get; set; }

    [Inject]
    public required IProductBuyRepository ProductBuyRepository { get; set; }

    [Inject]
    public required IPurchaseLogRepository PurchaseLogRepository { get; set; }

    [Inject]
    public required IProductRepository ProductRepository { get; set; }

    [Inject]
    public required ITenantProvider TenantProvider { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required UserState UserState { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    public List<ProductBuy> Products { get; set; } = [];
    public int Received { get; set; }

    private bool _initialized = false;
    private bool _isLoading = true;
    public bool IsLoading => _isLoading;

    protected override void OnInitialized()
    {
        UserState.OnStateChanged += OnUserStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _isLoading = true;
            StateHasChanged();
            await Task.Delay(100);

            if (!UserState.IsStoreUser || !UserState.HasTenant)
            {
                _isLoading = false;
                _initialized = true;
                StateHasChanged();
                return;
            }

            if (!string.IsNullOrEmpty(UserState.CurrentTenantId))
            {
                TenantProvider.SetTenantId(UserState.CurrentTenantId);
                await GetProducts();
            }

            _isLoading = false;
            _initialized = true;
            StateHasChanged();
        }
    }

    private async void OnUserStateChanged()
    {
        _isLoading = true;
        await InvokeAsync(StateHasChanged);

        if (UserState.HasTenant && !string.IsNullOrEmpty(UserState.CurrentTenantId))
        {
            TenantProvider.SetTenantId(UserState.CurrentTenantId);
            await GetProducts();
        }

        _isLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    public async Task GetProducts()
    {
        try
        {
            Products = await ProductBuyRepository.GetActiveProductBuy();
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add(ex.Message, Severity.Warning);
            Products = [];
        }
    }

    // 事前注文用QR表示
    private async Task ShowPreOrderQr()
    {
        var preOrderUrl = $"{NavigationManager.BaseUri}preorder/{UserState.CurrentTenantId}";

        var parameters = new DialogParameters
        {
            { nameof(PreOrderQrDialog.PreOrderUrl), preOrderUrl }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        await DialogService.ShowAsync<PreOrderQrDialog>("事前注文用QRコード", parameters, options);
    }

    // QRスキャナーを開く
    private async Task OpenQrScanner()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = false,
            CloseOnEscapeKey = false
        };

        var dialog = await DialogService.ShowAsync<QrScannerDialog>("QRコードを読み取る", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: string json })
        {
            await ApplyQrData(json);
        }
    }

    // QRデータを解析してレジに反映
    private async Task ApplyQrData(string json)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var payload = JsonSerializer.Deserialize<QrPayload>(json, options);

            if (payload == null || !payload.Items.Any())
            {
                Snackbar.Add("QRコードのデータが不正です", Severity.Error);
                return;
            }

            // テナントが一致するか確認
            if (payload.TenantId != UserState.CurrentTenantId)
            {
                Snackbar.Add("このQRコードは別のテナントのものです", Severity.Warning);
                return;
            }

            // 全数量をリセット
            foreach (var p in Products)
            {
                p.Amount = 0;
            }

            // QRの内容を反映
            var matched = 0;
            foreach (var item in payload.Items)
            {
                var product = Products.FirstOrDefault(p => p.Product.ProductId == item.ProductId);
                if (product != null)
                {
                    product.Amount = item.Quantity;
                    matched++;
                }
            }

            if (matched == 0)
            {
                Snackbar.Add("一致する商品が見つかりませんでした", Severity.Warning);
            }
            else
            {
                Snackbar.Add($"QRコードを読み取りました（{matched}種類）", Severity.Success);
            }

            StateHasChanged();
        }
        catch (JsonException)
        {
            Snackbar.Add("QRコードの形式が正しくありません", Severity.Error);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"エラー：{ex.Message}", Severity.Error);
        }
    }

    public async Task Purchase()
    {
        if (!UserState.HasTenant)
        {
            Snackbar.Add("テナントが選択されていません", Severity.Error);
            return;
        }

        var data = new List<PurchaseLog>();
        foreach (var product in Products)
        {
            if (product.Amount > 0)
            {
                data.Add(PurchaseLog.Create(product.Amount, product.Product.ProductId, null));
            }
        }

        if (data.Count > 0)
        {
            var ret = await PurchaseLogRepository.AddRangeAsync(data);
            if (ret != null && ret.IsSuccess)
            {
                // 在庫減算
                foreach (var product in Products.Where(p => p.Amount > 0))
                {
                    var stockResult = await ProductRepository.ReduceStockAsync(
                        product.Product.ProductId,
                        product.Amount
                    );
                    if (!stockResult.IsSuccess)
                    {
                        Console.WriteLine($"在庫更新失敗: {stockResult.ErrorMessage}");
                    }
                }

                Snackbar.Add("お買い上げありがとうございます！", Severity.Success);
                Received = 0;
            }
            else if (ret != null && !string.IsNullOrWhiteSpace(ret.ErrorMessage))
            {
                Snackbar.Add(ret.ErrorMessage, Severity.Error);
            }
            else
            {
                Snackbar.Add("何らかの問題が発生しました。購買スタッフまでお声がけください。", Severity.Error);
            }
            await GetProducts();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("購入情報を入力してください。");
        }
    }

    private void NavigateToLogin()
    {
        NavigationManager.NavigateTo("/login");
    }

    public void Dispose()
    {
        UserState.OnStateChanged -= OnUserStateChanged;
    }
}
