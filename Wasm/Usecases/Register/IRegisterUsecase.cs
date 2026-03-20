using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Usecases.Register;

public interface IRegisterUsecase
{
    Task<List<ProductBuy>> GetProductsAsync();
    Task<string?> GetProductImageAsync(string productId);
    Task<Result<PurchaseLog>> PurchaseAsync(List<PurchaseLog> items);
}
