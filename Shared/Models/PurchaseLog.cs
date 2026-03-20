namespace PurchaseWeb.Client.Models;

public class PurchaseLog
{
    public string LogId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public DateTime PurchaseDate { get; set; }
    public bool DeleteFlag { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
