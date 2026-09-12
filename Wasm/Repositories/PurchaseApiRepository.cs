using System.Net.Http.Json;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Services;

namespace PurchaseWeb.Wasm.Repositories;

public interface IPurchaseRepository
{
    /// <summary>購入を確定する。履歴の追加と在庫の減算はサーバー側で1トランザクション。</summary>
    Task<Result<PurchaseLog>> PurchaseAsync(List<PurchaseLog> items);
}

public class PurchaseApiRepository : ApiServiceBase, IPurchaseRepository
{
    public PurchaseApiRepository(HttpClient http, ITenantProvider tenantProvider)
        : base(http, tenantProvider) { }

    public async Task<Result<PurchaseLog>> PurchaseAsync(List<PurchaseLog> items)
    {
        using var req = CreateRequest(HttpMethod.Post, "/api/purchase");
        req.Content = JsonContent.Create(items);
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? Result<PurchaseLog>.Success(new PurchaseLog())
            : Result<PurchaseLog>.Failure(await res.Content.ReadAsStringAsync());
    }
}
