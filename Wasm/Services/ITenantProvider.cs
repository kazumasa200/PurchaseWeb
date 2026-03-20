namespace PurchaseWeb.Wasm.Services;

public interface ITenantProvider
{
    void SetTenantId(string tenantId);
    string? GetCurrentTenantId();
    bool HasTenantId();
}
