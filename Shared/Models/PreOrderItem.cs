namespace PurchaseWeb.Client.Models;

public class PreOrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Price { get; set; }
    public string? ImageBase64 { get; set; }
    public int? StockQuantity { get; set; }
    public int Quantity { get; set; }
    public int SubTotal => Price * Quantity;
}
