namespace PurchaseWeb.Client.Models;

/// <summary>
/// 商品 DTO（API の入出力に使用。画像は /api/products/{id}/image で別途取得）
/// </summary>
public class Product
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Price { get; set; }
    public string? Misc { get; set; }
    /// <summary>
    /// 登録・更新時のみセット。一覧取得では null（別テーブルで管理）
    /// </summary>
    public string? ImageBase64 { get; set; }
    public int? StockQuantity { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public bool DeleteFlag { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
