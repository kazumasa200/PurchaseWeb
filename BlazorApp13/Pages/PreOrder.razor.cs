using Infra.Persistance.Entities;
using Infra.Repositories;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QRCoder;
using System.Text.Json;

namespace PurchaseWeb.Pages;

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

    public List<PreOrderItem> OrderItems { get; set; } = [];

    public bool IsLoading { get; set; } = true;
    public bool TenantExists { get; set; } = false;
    public bool ShowQrCode { get; set; } = false;
    public string? QrCodeBase64 { get; set; }

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
            var products = await ProductRepository.GetActiveProducts();

            if (products != null)
            {
                TenantExists = true;
                OrderItems = products.Select(p => new PreOrderItem
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    ImageBase64 = p.ImageBase64,
                    StockQuantity = p.StockQuantity,
                    Quantity = 0
                }).ToList();
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

    private void GenerateQrCode()
    {
        if (!CartItems.Any()) return;

        // QRコードに埋め込むデータ
        var payload = new
        {
            tenantId = TenantId,
            items = CartItems.Select(x => new
            {
                productId = x.ProductId,
                quantity = x.Quantity
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload);

        // QRコード生成
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(json, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(10);
        QrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

        ShowQrCode = true;
        StateHasChanged();
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
