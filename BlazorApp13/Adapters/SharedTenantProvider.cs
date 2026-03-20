namespace PurchaseWeb.Adapters;

/// <summary>
/// API エンドポイントが X-Tenant-Id ヘッダーからテナントを設定するための
/// リクエストスコープ実装。サーバー側は Infra.ITenantProvider のみ実装する。
/// </summary>
public class SharedTenantProvider : Infra.Repositories.ITenantProvider
{
    private string? _currentTenantId;

    public string? GetCurrentTenantId() => _currentTenantId;
    public void SetTenantId(string tenantId) => _currentTenantId = tenantId;
    public bool HasTenantId() => !string.IsNullOrEmpty(_currentTenantId);
}
