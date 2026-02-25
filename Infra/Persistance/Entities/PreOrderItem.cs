namespace Infra.Persistance.Entities;

/// <summary>
/// 事前注文カートアイテム
/// </summary>
public class PreOrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Price { get; set; }
    public string? ImageBase64 { get; set; }
    public int? StockQuantity { get; set; }
    public int Quantity { get; set; } = 0;

    /// <summary>
    /// 小計
    /// </summary>
    public int SubTotal => Price * Quantity;
}
