using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Usecases.ProductManage;

public interface IProductManageUsecase
{
    Task<List<Product>> GetProductsAsync();
    Task<Dictionary<string, string?>> GetAllProductImagesAsync();
    Task<Result<Product>> CreateProductAsync(Product product);
    Task<Result<Product>> UpdateProductAsync(Product product);
    Task<Result<Product>> DeleteProductAsync(string productId);
}
