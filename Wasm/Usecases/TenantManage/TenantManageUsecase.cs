using PurchaseWeb.Client.Models;
using PurchaseWeb.Wasm.Repositories;

namespace PurchaseWeb.Wasm.Usecases.TenantManage;

public class TenantManageUsecase : ITenantManageUsecase
{
    private readonly ITenantRepository _repo;

    public TenantManageUsecase(ITenantRepository repo)
    {
        _repo = repo;
    }

    public Task<List<Tenant>> GetAllTenantsAsync()
        => _repo.GetAllTenantsAsync();

    public Task<List<Tenant>> GetActiveTenantsAsync()
        => _repo.GetActiveTenantsAsync();

    public Task<Result<Tenant>> CreateTenantAsync(string tenantName)
    {
        var tenant = new Tenant { TenantName = tenantName };
        return _repo.AddAsync(tenant);
    }

    public Task<Result<Tenant>> UpdateTenantAsync(Tenant tenant)
        => _repo.UpdateAsync(tenant);

    public async Task<Result<Tenant>> DeleteTenantAsync(Tenant tenant)
    {
        var deleted = new Tenant
        {
            TenantId   = tenant.TenantId,
            TenantName = tenant.TenantName,
            CreateDate = tenant.CreateDate,
            UpdateDate = DateTime.Now,
            DeleteFlag = true
        };
        return await _repo.UpdateAsync(deleted);
    }
}
