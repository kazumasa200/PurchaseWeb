using Infra.Persistance.Entities;
using Infra.Repositories;

namespace PurchaseWeb.Api.Usecases.PurchaseLogs;

public class PurchaseLogUsecase : IPurchaseLogUsecase
{
    private readonly IPurchaseLogRepository _repo;
    private readonly IProductRepository _productRepo;

    public PurchaseLogUsecase(IPurchaseLogRepository repo, IProductRepository productRepo)
    {
        _repo        = repo;
        _productRepo = productRepo;
    }

    public Task<List<PurchaseLog>> GetPurchaseLogsAsync()
        => _repo.GetPurchaseLogsAsync();

    public Task<Result<List<PurchaseLog>>> AddRangeAsync(List<PurchaseLog> logs)
        => _repo.AddRangeAsync(logs);

    public async Task<Result<PurchaseLog>> DeleteAsync(string logId)
    {
        var logs = await _repo.GetPurchaseLogsAsync();
        var target = logs.FirstOrDefault(l => l.LogId == logId);
        if (target == null) return Result<PurchaseLog>.Failure("購入ログが見つかりません");

        var deleted = PurchaseLog.Delete(target);
        var result = await _repo.DeleteAsync(deleted);

        if (result.IsSuccess)
        {
            // 在庫を元に戻す（null / -1 = 無制限は RestoreStockAsync 内で除外）
            await _productRepo.RestoreStockAsync(target.ProductId, target.Amount);
        }

        return result;
    }
}
