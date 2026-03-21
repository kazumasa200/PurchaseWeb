using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Helpers;
using PurchaseWeb.Wasm.Services;
using PurchaseWeb.Wasm.Usecases.Register;
using System.Text.Json;

namespace PurchaseWeb.Wasm.Pages;

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
    [Inject]
    public required IRegisterUsecase RegisterUsecase { get; set; }

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
    public int TotalSum { get; private set; }

    private bool _initialized = false;
    private bool _isLoading = true;
    public bool IsLoading => _isLoading;

    // 画像: data:image;base64,{...} 形式の表示用URLのみ保持（生base64は不要）
    private Dictionary<string, string?> _productImageSrcs = [];
    private HashSet<string> _imageLoadComplete = [];

    public bool IsImageLoaded(string productId) => _imageLoadComplete.Contains(productId);
    public string? GetProductImageSrc(string productId)
        => _productImageSrcs.GetValueOrDefault(productId);

    public void OnAmountChanged(ProductBuy item, int value)
    {
        item.Amount = value;
        TotalSum = Products.Sum(x => x.Sum);
    }

    protected override void OnInitialized()
    {
        UserState.OnStateChanged += OnUserStateChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            try
            {
                _isLoading = true;
                StateHasChanged();
                await Task.Delay(100);

                if (UserState.IsStoreUser && UserState.HasTenant &&
                    !string.IsNullOrEmpty(UserState.CurrentTenantId))
                {
                    TenantProvider.SetTenantId(UserState.CurrentTenantId);
                    await GetProducts();
                    _ = LoadImagesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Register OnAfterRenderAsync error: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                _initialized = true;
                StateHasChanged();
            }
        }
    }

    private async void OnUserStateChanged()
    {
        try
        {
            _isLoading = true;
            await InvokeAsync(StateHasChanged);

            if (UserState.HasTenant && !string.IsNullOrEmpty(UserState.CurrentTenantId))
            {
                TenantProvider.SetTenantId(UserState.CurrentTenantId);
                await GetProducts();
                _ = LoadImagesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"OnUserStateChanged error: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    public async Task GetProducts()
    {
        try
        {
            Products = await RegisterUsecase.GetProductsAsync();
            _productImageSrcs = [];
            _imageLoadComplete = [];
            TotalSum = 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GetProducts error: {ex.Message}");
            Snackbar.Add("商品の読み込みに失敗しました", Severity.Warning);
            Products = [];
            TotalSum = 0;
        }
    }

    private async Task LoadImagesAsync()
    {
        var productIds = Products.Select(p => p.Product.ProductId).ToList();
        foreach (var id in productIds)
        {
            try
            {
                string? base64;
                if (ImageCache.TryGet(id, out var cached))
                    base64 = cached;
                else
                {
                    base64 = await RegisterUsecase.GetProductImageAsync(id);
                    ImageCache.Set(id, base64);
                }
                _productImageSrcs[id] = base64 is null ? null : $"data:image;base64,{base64}";
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"LoadImagesAsync [{id}] error: {ex.Message}");
                _productImageSrcs[id] = null;
            }
            _imageLoadComplete.Add(id);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ShowPreOrderQr()
    {
        var preOrderUrl = $"{NavigationManager.BaseUri}preorder/{UserState.CurrentTenantId}";

        var parameters = new DialogParameters
        {
            { nameof(Component.PreOrderQrDialog.PreOrderUrl), preOrderUrl }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            CloseButton = true,
            CloseOnEscapeKey = true
        };

        await DialogService.ShowAsync<Component.PreOrderQrDialog>("事前注文用QRコード", parameters, options);
    }

    private async Task OpenQrScanner()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseButton = false,
            CloseOnEscapeKey = false
        };

        var dialog = await DialogService.ShowAsync<Component.QrScannerDialog>("QRコードを読み取る", options);
        var result = await dialog.Result;

        if (result is { Canceled: false, Data: string json })
        {
            await ApplyQrData(json);
        }
    }

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

            if (payload.TenantId != UserState.CurrentTenantId)
            {
                Snackbar.Add("このQRコードは別のテナントのものです", Severity.Warning);
                return;
            }

            foreach (var p in Products)
            {
                p.Amount = 0;
            }

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
                Snackbar.Add("一致する商品が見つかりませんでした", Severity.Warning);
            else
                Snackbar.Add($"QRコードを読み取りました（{matched}種類）", Severity.Success);

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

        var data = Products
            .Where(p => p.Amount > 0)
            .Select(p => new PurchaseLog
            {
                Amount = p.Amount,
                ProductId = p.Product.ProductId
            })
            .ToList();

        if (data.Count > 0)
        {
            var ret = await RegisterUsecase.PurchaseAsync(data);
            if (ret.IsSuccess)
            {
                Snackbar.Add("お買い上げありがとうございます！", Severity.Success);
                Received = 0;
            }
            else
            {
                Snackbar.Add(ret.ErrorMessage ?? "何らかの問題が発生しました。購買スタッフまでお声がけください。", Severity.Error);
            }
            await GetProducts();
            _ = LoadImagesAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add("購入情報を入力してください。");
        }
    }

    private static string GetStockText(int? stock) => StockDisplay.GetText(stock);
    private static Color GetStockColor(int? stock)  => StockDisplay.GetColor(stock);

    private void NavigateToLogin() => NavigationManager.NavigateTo("/login");

    public void Dispose()
    {
        UserState.OnStateChanged -= OnUserStateChanged;
    }
}
