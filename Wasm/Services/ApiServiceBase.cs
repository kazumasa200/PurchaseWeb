namespace PurchaseWeb.Wasm.Services;

public abstract class ApiServiceBase
{
    protected readonly HttpClient Http;
    private readonly ITenantProvider _tenantProvider;

    protected ApiServiceBase(HttpClient http, ITenantProvider tenantProvider)
    {
        Http = http;
        _tenantProvider = tenantProvider;
    }

    protected HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (!string.IsNullOrEmpty(tenantId))
            req.Headers.Add("X-Tenant-Id", tenantId);
        return req;
    }
}
