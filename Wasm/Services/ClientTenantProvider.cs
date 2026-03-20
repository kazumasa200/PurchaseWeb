namespace PurchaseWeb.Wasm.Services;

public class ClientTenantProvider : ITenantProvider
{
    private string? _tenantId;

    public void SetTenantId(string tenantId) => _tenantId = tenantId;
    public string? GetCurrentTenantId() => _tenantId;
    public bool HasTenantId() => !string.IsNullOrEmpty(_tenantId);
}
