using QRCoder;
using System.Text.Json;

namespace PurchaseWeb.Api;

public static class QrEndpoints
{
    public static void MapQrEndpoints(this WebApplication app)
    {
        // POST /api/qr/generate-cart  → カートデータ(JSON)からQRコード生成
        app.MapPost("/api/qr/generate-cart", (QrCartRequest req) =>
        {
            var json = JsonSerializer.Serialize(new
            {
                tenantId = req.TenantId,
                items = req.Items
            });
            return Results.Ok(GenerateQrBase64(json));
        });

        // POST /api/qr/generate-url  → URL からQRコード生成
        app.MapPost("/api/qr/generate-url", (QrUrlRequest req) =>
            Results.Ok(GenerateQrBase64(req.Url)));
    }

    private static string GenerateQrBase64(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData  = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode      = new PngByteQRCode(qrCodeData);
        return Convert.ToBase64String(qrCode.GetGraphic(10));
    }
}

public record QrCartItem(string ProductId, int Quantity);
public record QrCartRequest(string TenantId, List<QrCartItem> Items);
public record QrUrlRequest(string Url);
