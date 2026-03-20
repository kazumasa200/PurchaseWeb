using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Usecases.Login;

public interface ILoginUsecase
{
    Task<bool> ValidatePasswordAsync(string password);
    Task<List<Tenant>> GetActiveTenantsAsync();
}
