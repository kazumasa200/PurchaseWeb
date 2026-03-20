namespace PurchaseWeb.Client.Services;

/// <summary>
/// API サービス基底: X-Tenant-Id ヘッダーを自動付与する HttpClient ラッパー
/// </summary>
public abstract class ApiServiceBase
{
    protected readonly HttpClient Http;
    private readonly ITenantProvider _tenantProvider;

    protected ApiServiceBase(HttpClient http, ITenantProvider tenantProvider)
    {
        Http = http;
        _tenantProvider = tenantProvider;
    }

    /// <summary>X-Tenant-Id ヘッダーを含む HttpRequestMessage を生成</summary>
    protected HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!string.IsNullOrEmpty(tenantId))
            req.Headers.Add("X-Tenant-Id", tenantId);
        return req;
    }
}
