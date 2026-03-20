using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;
using PurchaseWeb.Wasm.Services;
using PurchaseWeb.Wasm.Usecases.PreOrder;
using System.Text.Json;
using ImageCache = PurchaseWeb.Wasm.Repositories.ImageCache;

namespace PurchaseWeb.Wasm.Pages;

public partial class PreOrder
{
    [Parameter]
    public string TenantId { get; set; } = string.Empty;

    [Inject]
    public required IPreOrderUsecase PreOrderUsecase { get; set; }

    [Inject]
    public required ITenantProvider TenantProvider { get; set; }

    [Inject]
    public required QrCodeService QrService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    public List<PreOrderItem> OrderItems { get; set; } = [];

    public bool IsLoading { get; set; } = true;
    public bool TenantExists { get; set; } = false;
    public bool ShowQrCode { get; set; } = false;
    public string? QrCodeBase64 { get; set; }
    private bool _isGeneratingQr = false;

    private Dictionary<string, string?> _productImages = [];
    private Dictionary<string, string?> _productImageSrcs = [];
    private HashSet<string> _imageLoadComplete = [];

    public bool IsImageLoaded(string productId) => _imageLoadComplete.Contains(productId);
    public string? GetProductImageSrc(string productId) => _productImageSrcs.GetValueOrDefault(productId);

    public List<PreOrderItem> CartItems => OrderItems.Where(x => x.Quantity > 0).ToList();
    public int TotalAmount => CartItems.Sum(x => x.SubTotal);

    protected override async Task OnInitializedAsync()
    {
        IsLoading = true;

        if (string.IsNullOrEmpty(TenantId))
        {
            IsLoading = false;
            return;
        }

        try
        {
            TenantProvider.SetTenantId(TenantId);
            var products = await PreOrderUsecase.GetProductsAsync();

            if (products != null)
            {
                TenantExists = true;
                OrderItems = products.Select(p => new PreOrderItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    ImageBase64 = null,
                    StockQuantity = p.StockQuantity,
                    Quantity = 0
                }).ToList();

                _productImages = [];
                _productImageSrcs = [];
                _imageLoadComplete = [];
            }
        }
        catch
        {
            TenantExists = false;
        }

        IsLoading = false;
        _ = LoadImagesAsync();
    }

    private async Task LoadImagesAsync()
    {
        var productIds = OrderItems.Select(x => x.ProductId).ToList();
        foreach (var id in productIds)
        {
            try
            {
                string? base64;
                if (ImageCache.TryGet(id, out var cached))
                {
                    base64 = cached;
                }
                else
                {
                    base64 = await PreOrderUsecase.GetProductImageAsync(id);
                    ImageCache.Set(id, base64);
                }
                _productImages[id] = base64;
                _productImageSrcs[id] = base64 is null ? null : $"data:image;base64,{base64}";
            }
            catch
            {
                _productImages[id] = null;
                _productImageSrcs[id] = null;
            }
            _imageLoadComplete.Add(id);
            await InvokeAsync(StateHasChanged);
        }
    }

    private void IncreaseQuantity(PreOrderItem item)
    {
        item.Quantity++;
        StateHasChanged();
    }

    private void DecreaseQuantity(PreOrderItem item)
    {
        if (item.Quantity > 0)
        {
            item.Quantity--;
            StateHasChanged();
        }
    }

    private Task GenerateQrCode()
    {
        if (!CartItems.Any()) return Task.CompletedTask;

        _isGeneratingQr = true;
        ShowQrCode = true;

        try
        {
            // カートの内容をJSONにシリアライズしてQRを生成（クライアント側、API不要）
            var payload = new
            {
                TenantId,
                Items = CartItems.Select(x => new { x.ProductId, x.Quantity })
            };
            var json = JsonSerializer.Serialize(payload);
            QrCodeBase64 = QrService.GenerateQrBase64(json);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"QRコード生成エラー: {ex.Message}", Severity.Error);
            ShowQrCode = false;
        }
        finally
        {
            _isGeneratingQr = false;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private void BackToOrder()
    {
        ShowQrCode = false;
        QrCodeBase64 = null;
        StateHasChanged();
    }

    private static string GetStockText(int? stock) => stock switch
    {
        null => "在庫：無制限",
        0    => "在庫なし",
        <= 5 => $"残り {stock} 個",
        _    => $"在庫：{stock} 個"
    };

    private static Color GetStockColor(int? stock) => stock switch
    {
        null => Color.Default,
        0    => Color.Error,
        <= 5 => Color.Warning,
        _    => Color.Success
    };
}
