using Infra.Persistance.Entities;
using Infra.Repositories;

namespace PurchaseWeb.Api.Usecases.PurchaseLogs;

public class PurchaseLogUsecase : IPurchaseLogUsecase
{
    private readonly IPurchaseLogRepository _repo;

    public PurchaseLogUsecase(IPurchaseLogRepository repo)
    {
        _repo = repo;
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
        return await _repo.DeleteAsync(deleted);
    }
}
