namespace Infra.Persistance.Entities;

/// <summary>
/// 商品画像（Products テーブルから分離して格納）
/// </summary>
public class ProductImage
{
    public string ImageId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string? ImageBase64 { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product? Product { get; set; }
}
