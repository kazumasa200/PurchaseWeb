using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.Register;

public class RegisterUsecase : IRegisterUsecase
{
    private readonly IProductBuyRepository _productBuyRepo;
    private readonly IPurchaseLogRepository _purchaseLogRepo;
    private readonly IProductRepository _productRepo;
    private readonly IQrRepository _qrRepo;

    public RegisterUsecase(
        IProductBuyRepository productBuyRepo,
        IPurchaseLogRepository purchaseLogRepo,
        IProductRepository productRepo,
        IQrRepository qrRepo)
    {
        _productBuyRepo  = productBuyRepo;
        _purchaseLogRepo = purchaseLogRepo;
        _productRepo     = productRepo;
        _qrRepo          = qrRepo;
    }

    public Task<List<ProductBuy>> GetProductsAsync()
        => _productBuyRepo.GetActiveProductBuyWithoutImages();

    public Task<Dictionary<string, string?>> GetAllProductImagesAsync()
        => _productRepo.GetAllProductImagesAsync();

    public async Task<Result<PurchaseLog>> PurchaseAsync(List<PurchaseLog> items)
    {
        var ret = await _purchaseLogRepo.AddRangeAsync(items);
        if (!ret.IsSuccess) return ret;

        foreach (var item in items)
        {
            await _productRepo.ReduceStockAsync(item.ProductId, item.Amount);
        }

        return Result<PurchaseLog>.Success(new PurchaseLog());
    }

    public Task<string?> GeneratePreOrderQrAsync(string url)
        => _qrRepo.GenerateUrlQrAsync(url);
}
