using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetActiveProductsWithoutImages();
    Task<List<Product>> GetAllProducts();
    Task<string?> GetProductImageAsync(string productId);
    Task<Dictionary<string, string?>> GetAllProductImagesAsync();
    Task<Result<Product>> AddAsync(Product product);
    Task<Result<Product>> UpdateAsync(Product product);
    Task<Result<Product>> DeleteAsync(string productId);
    Task<Product?> GetByIdAsync(string productId);
    Task<Result<Product>> ReduceStockAsync(string productId, int quantity);
}

public interface IProductBuyRepository
{
    Task<List<ProductBuy>> GetActiveProductBuyWithoutImages();
}

public interface IPurchaseLogRepository
{
    Task<List<PurchaseLog>> GetPurchaseLogsAsync();
    Task<Result<PurchaseLog>> AddRangeAsync(List<PurchaseLog> logs);
    Task<Result<PurchaseLog>> DeleteAsync(string logId);
}

public interface ITenantRepository
{
    Task<List<Tenant>> GetActiveTenantsAsync();
    Task<List<Tenant>> GetAllTenantsAsync();
    Task<Result<Tenant>> AddAsync(Tenant tenant);
    Task<Result<Tenant>> UpdateAsync(Tenant tenant);
}

public interface IAuthRepository
{
    Task<bool> ValidatePasswordAsync(string password);

    /// <summary>いま店員として認証されているかをサーバーに聞く（localStorage は信用しない）</summary>
    Task<bool> IsAuthenticatedAsync();

    Task LogoutAsync();
}

public interface IQrRepository
{
    Task<string?> GenerateCartQrAsync(string tenantId, List<QrCartItem> items);
    Task<string?> GenerateUrlQrAsync(string url);
}

public record QrCartItem(string ProductId, int Quantity);
