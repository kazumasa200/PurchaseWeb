using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.PurchaseHistory;

public class PurchaseHistoryUsecase : IPurchaseHistoryUsecase
{
    private readonly IPurchaseLogRepository _logRepo;
    private readonly IProductRepository _productRepo;

    public PurchaseHistoryUsecase(IPurchaseLogRepository logRepo, IProductRepository productRepo)
    {
        _logRepo     = logRepo;
        _productRepo = productRepo;
    }

    public async Task<(List<PurchaseLog> Logs, List<Product> Products)> LoadDataAsync()
    {
        var logs     = await _logRepo.GetPurchaseLogsAsync();
        var products = await _productRepo.GetAllProducts();
        return (logs, products);
    }

    public Task<Result<PurchaseLog>> DeleteLogAsync(string logId)
        => _logRepo.DeleteAsync(logId);
}
