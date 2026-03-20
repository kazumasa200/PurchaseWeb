using QRCoder;
using System.Text.Json;

namespace PurchaseWeb.Api.Usecases.Qr;

public class QrUsecase : IQrUsecase
{
    public string GenerateCartQr(string tenantId, List<QrCartItem> items)
    {
        var json = JsonSerializer.Serialize(new { tenantId, items });
        return GenerateQrBase64(json);
    }

    public string GenerateUrlQr(string url)
        => GenerateQrBase64(url);

    private static string GenerateQrBase64(string content)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData  = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qrCode      = new PngByteQRCode(qrCodeData);
        return Convert.ToBase64String(qrCode.GetGraphic(10));
    }
}
