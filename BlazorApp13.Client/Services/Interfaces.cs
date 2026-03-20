using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Client.Services;

public interface ILMStudioService
{
    Task<List<LMStudioModel>> GetModelsAsync();
    IAsyncEnumerable<string> StreamMessagesAsync(string modelId, List<ChatMessage> chatHistory, ScrollToBottomContext scrollToBottomContext);
}


public interface IProductRepository
{
    Task<List<Product>> GetActiveProducts();
    Task<List<Product>> GetActiveProductsWithoutImages();
    Task<List<Product>> GetAllProducts();
    Task<string?> GetProductImageAsync(string productId);
    Task<Result<Product>> AddAsync(Product product);
    Task<Result<Product>> UpdateAsync(Product product);
    Task<Result<Product>> DeleteAsync(string productId);
    Task<Product?> GetByIdAsync(string productId);
    Task<Result<Product>> ReduceStockAsync(string productId, int quantity);
}

public interface IProductBuyRepository
{
    Task<List<ProductBuy>> GetActiveProductBuy();
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

public interface ITenantProvider
{
    void SetTenantId(string tenantId);
    string? GetCurrentTenantId();
    bool HasTenantId();
}
