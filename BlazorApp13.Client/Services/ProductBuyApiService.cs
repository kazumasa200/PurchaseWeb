using System.Net.Http.Json;
using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Client.Services;

public class ProductBuyApiService : ApiServiceBase, IProductBuyRepository
{
    public ProductBuyApiService(HttpClient http, ITenantProvider tenantProvider)
        : base(http, tenantProvider) { }

    public async Task<List<ProductBuy>> GetActiveProductBuy()
    {
        using var req = CreateRequest(HttpMethod.Get, "/api/productbuy?withImages=true");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<List<ProductBuy>>() ?? []
            : [];
    }

    public async Task<List<ProductBuy>> GetActiveProductBuyWithoutImages()
    {
        using var req = CreateRequest(HttpMethod.Get, "/api/productbuy");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<List<ProductBuy>>() ?? []
            : [];
    }
}
