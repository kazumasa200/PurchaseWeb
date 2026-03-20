using Infra.Persistance.Entities;
using Infra.Repositories;

namespace PurchaseWeb.Api.Usecases.Products;

public class ProductUsecase : IProductUsecase
{
    private readonly IProductRepository _productRepo;

    public ProductUsecase(IProductRepository productRepo)
    {
        _productRepo = productRepo;
    }

    public Task<List<Product>> GetActiveProductsWithoutImagesAsync()
        => _productRepo.GetActiveProductsWithoutImages();

    public Task<List<Product>> GetAllProductsAsync()
        => _productRepo.GetAllProducts();

    public Task<Product?> GetByIdAsync(string productId)
        => _productRepo.GetByIdAsync(productId);

    public Task<string?> GetProductImageAsync(string productId)
        => _productRepo.GetProductImageAsync(productId);

    public Task<Dictionary<string, string?>> GetAllProductImagesAsync()
        => _productRepo.GetAllProductImagesAsync();

    public async Task<Result<Product>> CreateAsync(Product product, string? imageBase64)
    {
        var ret = await _productRepo.AddAsync(product);
        if (!ret.IsSuccess) return ret;

        if (!string.IsNullOrEmpty(imageBase64))
            await _productRepo.SaveImageAsync(ret.Data!.ProductId, imageBase64);

        return ret;
    }

    public async Task<Result<Product>> UpdateAsync(Product product, string? imageBase64)
    {
        var ret = await _productRepo.UpdateAsync(product);
        if (!ret.IsSuccess) return ret;

        if (imageBase64 != null)
            await _productRepo.SaveImageAsync(product.ProductId, imageBase64);

        return ret;
    }

    public async Task<Result<Product>> DeleteAsync(string productId)
    {
        var existing = await _productRepo.GetByIdAsync(productId);
        if (existing == null) return Result<Product>.Failure("商品が見つかりません");
        var deleted = Product.Delete(existing);
        return await _productRepo.UpdateAsync(deleted);
    }

    public Task<Result<Product>> ReduceStockAsync(string productId, int quantity)
        => _productRepo.ReduceStockAsync(productId, quantity);
}
