using System.Net.Http.Json;
using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Client.Services;

public class ProductApiService : ApiServiceBase, IProductRepository
{
    public ProductApiService(HttpClient http, ITenantProvider tenantProvider)
        : base(http, tenantProvider) { }

    public async Task<List<Product>> GetActiveProducts()
    {
        using var req = CreateRequest(HttpMethod.Get, "/api/products?withImages=true");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<List<Product>>() ?? []
            : [];
    }

    public async Task<List<Product>> GetAllProducts()
    {
        using var req = CreateRequest(HttpMethod.Get, "/api/products/all");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<List<Product>>() ?? []
            : [];
    }

    public async Task<List<Product>> GetActiveProductsWithoutImages()
    {
        using var req = CreateRequest(HttpMethod.Get, "/api/products");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<List<Product>>() ?? []
            : [];
    }

    public async Task<string?> GetProductImageAsync(string productId)
    {
        using var req = CreateRequest(HttpMethod.Get, $"/api/products/{productId}/image");
        var res = await Http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return null;
        var body = await res.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(body) ? null : body.Trim('"');
    }

    public async Task<Result<Product>> AddAsync(Product product)
    {
        using var req = CreateRequest(HttpMethod.Post, "/api/products");
        req.Content = JsonContent.Create(product);
        var res = await Http.SendAsync(req);
        if (res.IsSuccessStatusCode)
            return Result<Product>.Success(await res.Content.ReadFromJsonAsync<Product>() ?? product);
        var err = await res.Content.ReadAsStringAsync();
        return Result<Product>.Failure(err);
    }

    public async Task<Result<Product>> UpdateAsync(Product product)
    {
        using var req = CreateRequest(HttpMethod.Put, $"/api/products/{product.ProductId}");
        req.Content = JsonContent.Create(product);
        var res = await Http.SendAsync(req);
        if (res.IsSuccessStatusCode)
            return Result<Product>.Success(product);
        var err = await res.Content.ReadAsStringAsync();
        return Result<Product>.Failure(err);
    }

    public async Task<Result<Product>> DeleteAsync(string productId)
    {
        using var req = CreateRequest(HttpMethod.Delete, $"/api/products/{productId}");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? Result<Product>.Success(new Product())
            : Result<Product>.Failure(await res.Content.ReadAsStringAsync());
    }

    public async Task<Product?> GetByIdAsync(string productId)
    {
        using var req = CreateRequest(HttpMethod.Get, $"/api/products/{productId}");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? await res.Content.ReadFromJsonAsync<Product>()
            : null;
    }

    public async Task<Result<Product>> ReduceStockAsync(string productId, int quantity)
    {
        using var req = CreateRequest(HttpMethod.Post, $"/api/products/{productId}/reduce-stock?quantity={quantity}");
        var res = await Http.SendAsync(req);
        return res.IsSuccessStatusCode
            ? Result<Product>.Success(new Product())
            : Result<Product>.Failure(await res.Content.ReadAsStringAsync());
    }
}
