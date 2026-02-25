namespace Infra.Repositories;

public interface ITenantProvider
{
    string? GetCurrentTenantId();

    void SetTenantId(string tenantId);

    bool HasTenantId();  // 追加
}

public class TenantProvider : ITenantProvider
{
    private string? _currentTenantId;

    public string? GetCurrentTenantId()
    {
        return _currentTenantId;  // エラーを投げずにnullを返す
    }

    public void SetTenantId(string tenantId)
    {
        _currentTenantId = tenantId;
    }

    public bool HasTenantId()
    {
        return !string.IsNullOrEmpty(_currentTenantId);
    }
}
