using System.Net.Http.Json;
using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Services;

namespace PurchaseWeb.Wasm.Repositories;

public class ProductBuyApiRepository : ApiServiceBase, IProductBuyRepository
{
    public ProductBuyApiRepository(HttpClient http, ITenantProvider tenantProvider)
        : base(http, tenantProvider) { }

    public async Task<List<ProductBuy>> GetActiveProductBuyWithoutImages()
    {
        using var req = CreateRequest(HttpMethod.Get, "/api/productbuy");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<List<ProductBuy>>() ?? []
            : [];
    }
}
