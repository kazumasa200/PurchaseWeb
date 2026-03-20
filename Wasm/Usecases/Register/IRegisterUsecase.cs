using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Usecases.Register;

public interface IRegisterUsecase
{
    Task<List<ProductBuy>> GetProductsAsync();
    Task<Dictionary<string, string?>> GetAllProductImagesAsync();
    Task<Result<PurchaseLog>> PurchaseAsync(List<PurchaseLog> items);
    Task<string?> GeneratePreOrderQrAsync(string url);
}
