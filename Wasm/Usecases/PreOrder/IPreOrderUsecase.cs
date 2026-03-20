using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Usecases.PreOrder;

public interface IPreOrderUsecase
{
    Task<List<Product>> GetProductsAsync();
    Task<string?> GetProductImageAsync(string productId);
}
