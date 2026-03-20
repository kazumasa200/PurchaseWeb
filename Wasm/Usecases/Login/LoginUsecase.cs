using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.Login;

public class LoginUsecase : ILoginUsecase
{
    private readonly IAuthRepository _authRepo;
    private readonly ITenantRepository _tenantRepo;

    public LoginUsecase(IAuthRepository authRepo, ITenantRepository tenantRepo)
    {
        _authRepo   = authRepo;
        _tenantRepo = tenantRepo;
    }

    public Task<bool> ValidatePasswordAsync(string password)
        => _authRepo.ValidatePasswordAsync(password);

    public Task<List<Tenant>> GetActiveTenantsAsync()
        => _tenantRepo.GetActiveTenantsAsync();
}
