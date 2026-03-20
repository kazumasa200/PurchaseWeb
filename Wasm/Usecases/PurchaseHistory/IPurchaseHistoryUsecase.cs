using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Usecases.PurchaseHistory;

public interface IPurchaseHistoryUsecase
{
    Task<(List<PurchaseLog> Logs, List<Product> Products)> LoadDataAsync();
    Task<Result<PurchaseLog>> DeleteLogAsync(string logId);
}
