using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.ProductManage;

public class ProductManageUsecase : IProductManageUsecase
{
    private readonly IProductRepository _productRepo;

    public ProductManageUsecase(IProductRepository productRepo)
    {
        _productRepo = productRepo;
    }

    public Task<List<Product>> GetProductsAsync()
        => _productRepo.GetActiveProductsWithoutImages();

    public Task<Dictionary<string, string?>> GetAllProductImagesAsync()
        => _productRepo.GetAllProductImagesAsync();

    public Task<Result<Product>> CreateProductAsync(Product product)
        => _productRepo.AddAsync(product);

    public Task<Result<Product>> UpdateProductAsync(Product product)
        => _productRepo.UpdateAsync(product);

    public Task<Result<Product>> DeleteProductAsync(string productId)
        => _productRepo.DeleteAsync(productId);
}
