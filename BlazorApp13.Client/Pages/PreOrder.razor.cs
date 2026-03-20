using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Client.Services;
using System.Net.Http.Json;

namespace PurchaseWeb.Client.Pages;

public partial class PreOrder
{
    [Parameter]
    public string TenantId { get; set; } = string.Empty;

    [Inject]
    public required IProductRepository ProductRepository { get; set; }

    [Inject]
    public required ITenantProvider TenantProvider { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required HttpClient Http { get; set; }

    public List<PreOrderItem> OrderItems { get; set; } = [];

    public bool IsLoading { get; set; } = true;
    public bool TenantExists { get; set; } = false;
    public bool ShowQrCode { get; set; } = false;
    public string? QrCodeBase64 { get; set; }
    private bool _isGeneratingQr = false;

    // 画像の非同期読み込み状態管理
    private Dictionary<string, string?> _productImages = [];
    private HashSet<string> _imageLoadComplete = [];

    public bool IsImageLoaded(string productId) => _imageLoadComplete.Contains(productId);
    public string? GetProductImage(string productId) => _productImages.GetValueOrDefault(productId);

    // カートに入っているもの（数量>0）
    public List<PreOrderItem> CartItems => OrderItems.Where(x => x.Quantity > 0).ToList();

    // 合計金額
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
            var products = await ProductRepository.GetActiveProductsWithoutImages();

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
        var targets = OrderItems.ToList();
        foreach (var item in targets)
        {
            try
            {
                var image = await ProductRepository.GetProductImageAsync(item.ProductId);
                _productImages[item.ProductId] = image;
            }
            catch
            {
                _productImages[item.ProductId] = null;
            }
            _imageLoadComplete.Add(item.ProductId);
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

    private async Task GenerateQrCode()
    {
        if (!CartItems.Any()) return;

        _isGeneratingQr = true;
        ShowQrCode = true;
        StateHasChanged();

        try
        {
            var payload = new
            {
                tenantId = TenantId,
                items = CartItems.Select(x => new
                {
                    productId = x.ProductId,
                    quantity = x.Quantity
                }).ToList()
            };

            var response = await Http.PostAsJsonAsync("/api/qr/generate-cart", payload);
            if (response.IsSuccessStatusCode)
            {
                QrCodeBase64 = await response.Content.ReadFromJsonAsync<string>();
            }
            else
            {
                Snackbar.Add("QRコードの生成に失敗しました", Severity.Error);
                ShowQrCode = false;
            }
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
    }

    private void BackToOrder()
    {
        ShowQrCode = false;
        QrCodeBase64 = null;
        StateHasChanged();
    }

    private string GetStockText(int? stock)
    {
        if (stock == null) return "在庫：無制限";
        if (stock == 0) return "在庫なし";
        if (stock <= 5) return $"在庫：残り {stock} 個";
        return $"在庫あり：およそ {stock} 個";
    }

    private Color GetStockColor(int? stock)
    {
        if (stock == null) return Color.Success;
        if (stock == 0) return Color.Error;
        if (stock <= 5) return Color.Warning;
        return Color.Success;
    }
}
