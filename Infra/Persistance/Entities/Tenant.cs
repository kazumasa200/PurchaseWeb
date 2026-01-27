namespace Infra.Persistance.Entities;

public class Tenant
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public bool DeleteFlag { get; set; }

    public static Tenant Create(string tenantName)
    {
        return new Tenant
        {
            TenantId = Guid.NewGuid().ToString(),
            TenantName = tenantName,
            CreateDate = DateTime.UtcNow,
            DeleteFlag = false
        };
    }

    public static Tenant Update(Tenant tenant, string newName)
    {
        if (string.IsNullOrEmpty(newName))
            throw new Exception("テナント名は必須です");

        return new Tenant
        {
            TenantId = tenant.TenantId,
            TenantName = newName,
            CreateDate = tenant.CreateDate,
            UpdateDate = DateTime.Now,
            DeleteFlag = tenant.DeleteFlag
        };
    }
}
