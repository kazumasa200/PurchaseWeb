using System.Net.Http.Json;
using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Client.Services;

public class PurchaseLogApiService : ApiServiceBase, IPurchaseLogRepository
{
    public PurchaseLogApiService(HttpClient http, ITenantProvider tenantProvider)
        : base(http, tenantProvider) { }

    public async Task<List<PurchaseLog>> GetPurchaseLogsAsync()
    {
        using var req = CreateRequest(HttpMethod.Get, "/api/purchaselogs");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<List<PurchaseLog>>() ?? []
            : [];
    }

    public async Task<Result<PurchaseLog>> AddRangeAsync(List<PurchaseLog> logs)
    {
        using var req = CreateRequest(HttpMethod.Post, "/api/purchaselogs");
        req.Content = JsonContent.Create(logs);
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? Result<PurchaseLog>.Success(new PurchaseLog())
            : Result<PurchaseLog>.Failure(await res.Content.ReadAsStringAsync());
    }

    public async Task<Result<PurchaseLog>> DeleteAsync(string logId)
    {
        using var req = CreateRequest(HttpMethod.Delete, $"/api/purchaselogs/{logId}");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? Result<PurchaseLog>.Success(new PurchaseLog())
            : Result<PurchaseLog>.Failure(await res.Content.ReadAsStringAsync());
    }
}
