using Infra.Persistance.Entities;
using Infra.Repositories;

namespace PurchaseWeb.Api.Usecases.Tenants;

public class TenantUsecase : ITenantUsecase
{
    private readonly ITenantRepository _repo;

    public TenantUsecase(ITenantRepository repo)
    {
        _repo = repo;
    }

    public Task<List<Tenant>> GetActiveTenantsAsync()
        => _repo.GetActiveTenantsAsync();

    public Task<List<Tenant>> GetAllTenantsAsync()
        => _repo.GetAllTenantsAsync();

    public Task<Result<Tenant>> CreateAsync(Tenant tenant)
        => _repo.AddAsync(tenant);

    public Task<Result<Tenant>> UpdateAsync(Tenant tenant)
        => _repo.UpdateAsync(tenant);
}
