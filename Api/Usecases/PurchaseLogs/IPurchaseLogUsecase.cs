using Infra.Persistance.Entities;

namespace PurchaseWeb.Api.Usecases.PurchaseLogs;

public interface IPurchaseLogUsecase
{
    Task<List<PurchaseLog>> GetPurchaseLogsAsync();
    Task<Result<List<PurchaseLog>>> AddRangeAsync(List<PurchaseLog> logs);
    Task<Result<PurchaseLog>> DeleteAsync(string logId);
}
