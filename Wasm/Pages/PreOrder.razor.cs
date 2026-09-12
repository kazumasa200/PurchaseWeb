using Microsoft.AspNetCore.Components;
using MudBlazor;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Helpers;
using PurchaseWeb.Wasm.Repositories;
using PurchaseWeb.Wasm.Services;
using PurchaseWeb.Wasm.Usecases.PreOrder;
using System.Text.Json;

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

    /// <summary>
    /// 画像の取得先。ブラウザが直接叩くので、ヘッダを使えない代わりにテナントをクエリで渡す。
    /// 以前はここで全商品の base64 を先に取ってメモリに載せていた。
    /// </summary>
    public string GetProductImageUrl(string productId)
        => $"/api/products/{productId}/photo?tenantId={Uri.EscapeDataString(TenantId)}";

    // カート: Increase/DecreaseQuantity のたびに更新してレンダリングごとの再計算を避ける
    private List<PreOrderItem> _cartItems = [];
    private int _totalAmount;
    public IReadOnlyList<PreOrderItem> CartItems => _cartItems;
    public int TotalAmount => _totalAmount;

    private void RefreshCart()
    {
        _cartItems  = OrderItems.Where(x => x.Quantity > 0).ToList();
        _totalAmount = _cartItems.Sum(x => x.SubTotal);
    }

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

                RefreshCart();
            }
        }
        catch
        {
            TenantExists = false;
        }

        IsLoading = false;
    }


    private void IncreaseQuantity(PreOrderItem item)
    {
        item.Quantity++;
        RefreshCart();
        StateHasChanged();
    }

    private void DecreaseQuantity(PreOrderItem item)
    {
        if (item.Quantity > 0)
        {
            item.Quantity--;
            RefreshCart();
            StateHasChanged();
        }
    }

    private void GenerateQrCode()
    {
        if (!CartItems.Any()) return;

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
    }

    private void BackToOrder()
    {
        ShowQrCode = false;
        QrCodeBase64 = null;
        StateHasChanged();
    }

    private static string GetStockText(int? stock) => StockDisplay.GetText(stock);
    private static Color GetStockColor(int? stock)  => StockDisplay.GetColor(stock);
}
