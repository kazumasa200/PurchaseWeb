namespace PurchaseWeb.Client.Models;

public class Tenant
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public bool DeleteFlag { get; set; }
}
