using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.PreOrder;

public class PreOrderUsecase : IPreOrderUsecase
{
    private readonly IProductRepository _productRepo;

    public PreOrderUsecase(IProductRepository productRepo)
        => _productRepo = productRepo;

    public Task<List<Product>> GetProductsAsync()
        => _productRepo.GetActiveProductsWithoutImages();

    public Task<string?> GetProductImageAsync(string productId)
        => _productRepo.GetProductImageAsync(productId);
}
