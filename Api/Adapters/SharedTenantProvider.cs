namespace PurchaseWeb.Api.Adapters;

public class SharedTenantProvider : Infra.Repositories.ITenantProvider
{
    private string? _currentTenantId;

    public string? GetCurrentTenantId() => _currentTenantId;
    public void SetTenantId(string tenantId) => _currentTenantId = tenantId;
    public bool HasTenantId() => !string.IsNullOrEmpty(_currentTenantId);
}
