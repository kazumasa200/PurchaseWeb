using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.Register;

public class RegisterUsecase : IRegisterUsecase
{
    private readonly IProductBuyRepository _productBuyRepo;
    private readonly IPurchaseLogRepository _purchaseLogRepo;
    private readonly IProductRepository _productRepo;

    public RegisterUsecase(
        IProductBuyRepository productBuyRepo,
        IPurchaseLogRepository purchaseLogRepo,
        IProductRepository productRepo)
    {
        _productBuyRepo  = productBuyRepo;
        _purchaseLogRepo = purchaseLogRepo;
        _productRepo     = productRepo;
    }

    public Task<List<ProductBuy>> GetProductsAsync()
        => _productBuyRepo.GetActiveProductBuyWithoutImages();

    public Task<string?> GetProductImageAsync(string productId)
        => _productRepo.GetProductImageAsync(productId);

    public async Task<Result<PurchaseLog>> PurchaseAsync(List<PurchaseLog> items)
    {
        var ret = await _purchaseLogRepo.AddRangeAsync(items);
        if (!ret.IsSuccess) return ret;

        foreach (var item in items)
            await _productRepo.ReduceStockAsync(item.ProductId, item.Amount);

        return Result<PurchaseLog>.Success(new PurchaseLog());
    }
}
