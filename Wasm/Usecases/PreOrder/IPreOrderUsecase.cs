using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.PreOrder;

public interface IPreOrderUsecase
{
    Task<List<Product>> GetProductsAsync();
    Task<string?> GetProductImageAsync(string productId);
    Task<string?> GenerateCartQrAsync(string tenantId, List<QrCartItem> items);
}
