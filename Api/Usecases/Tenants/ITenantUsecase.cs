using Infra.Persistance.Entities;

namespace PurchaseWeb.Api.Usecases.Tenants;

public interface ITenantUsecase
{
    Task<List<Tenant>> GetActiveTenantsAsync();
    Task<List<Tenant>> GetAllTenantsAsync();
    Task<Result<Tenant>> CreateAsync(Tenant tenant);
    Task<Result<Tenant>> UpdateAsync(Tenant tenant);
}
