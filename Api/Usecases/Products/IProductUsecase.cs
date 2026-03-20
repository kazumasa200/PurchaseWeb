using Infra.Persistance.Entities;

namespace PurchaseWeb.Api.Usecases.Products;

public interface IProductUsecase
{
    Task<List<Product>> GetActiveProductsWithoutImagesAsync();
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetByIdAsync(string productId);
    Task<string?> GetProductImageAsync(string productId);
    Task<Dictionary<string, string?>> GetAllProductImagesAsync();
    Task<Result<Product>> CreateAsync(Product product, string? imageBase64);
    Task<Result<Product>> UpdateAsync(Product product, string? imageBase64);
    Task<Result<Product>> DeleteAsync(string productId);
    Task<Result<Product>> ReduceStockAsync(string productId, int quantity);
}
