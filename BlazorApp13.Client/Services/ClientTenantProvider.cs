namespace PurchaseWeb.Client.Services;

/// <summary>
/// WASM 側のテナント保持 (メモリのみ)
/// </summary>
public class ClientTenantProvider : ITenantProvider
{
    private string? _tenantId;

    public void SetTenantId(string tenantId) => _tenantId = tenantId;
    public string? GetCurrentTenantId() => _tenantId;
    public bool HasTenantId() => !string.IsNullOrEmpty(_tenantId);
}
