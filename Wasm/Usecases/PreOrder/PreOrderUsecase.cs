using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.PreOrder;

public class PreOrderUsecase : IPreOrderUsecase
{
    private readonly IProductRepository _productRepo;
    private readonly IQrRepository _qrRepo;

    public PreOrderUsecase(IProductRepository productRepo, IQrRepository qrRepo)
    {
        _productRepo = productRepo;
        _qrRepo      = qrRepo;
    }

    public Task<List<Product>> GetProductsAsync()
        => _productRepo.GetActiveProductsWithoutImages();

    public Task<string?> GetProductImageAsync(string productId)
        => _productRepo.GetProductImageAsync(productId);

    public Task<string?> GenerateCartQrAsync(string tenantId, List<QrCartItem> items)
        => _qrRepo.GenerateCartQrAsync(tenantId, items);
}
