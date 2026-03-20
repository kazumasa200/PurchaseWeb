using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Usecases.TenantManage;

public interface ITenantManageUsecase
{
    Task<List<Tenant>> GetAllTenantsAsync();
    Task<List<Tenant>> GetActiveTenantsAsync();
    Task<Result<Tenant>> CreateTenantAsync(string tenantName);
    Task<Result<Tenant>> UpdateTenantAsync(Tenant tenant);
    Task<Result<Tenant>> DeleteTenantAsync(Tenant tenant);
}
