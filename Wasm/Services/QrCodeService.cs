using QRCoder;

namespace PurchaseWeb.Wasm.Services;

/// <summary>クライアント側でQRコードを生成するサービス（API不要）</summary>
public class QrCodeService
{
    /// <summary>
    /// コンテンツをQRコード化し、PNGのBase64文字列を返す（data:プレフィックスなし）
    /// </summary>
    public string GenerateQrBase64(string content)
    {
        using var generator = new QRCodeGenerator();
        var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(10); // 10px per module
        return Convert.ToBase64String(bytes);
    }
}
