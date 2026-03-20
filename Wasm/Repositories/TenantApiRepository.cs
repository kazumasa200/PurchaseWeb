using System.Net.Http.Json;
using PurchaseWeb.Client.Models;

namespace PurchaseWeb.Wasm.Repositories;

public class TenantApiRepository : ITenantRepository
{
    private readonly HttpClient _http;

    public TenantApiRepository(HttpClient http) => _http = http;

    public async Task<List<Tenant>> GetActiveTenantsAsync()
        => await _http.GetFromJsonAsync<List<Tenant>>("/api/tenants") ?? [];

    public async Task<List<Tenant>> GetAllTenantsAsync()
        => await _http.GetFromJsonAsync<List<Tenant>>("/api/tenants/all") ?? [];

    public async Task<Result<Tenant>> AddAsync(Tenant tenant)
    {
        var res = await _http.PostAsJsonAsync("/api/tenants", tenant);
        if (res.IsSuccessStatusCode)
            return Result<Tenant>.Success(await res.Content.ReadFromJsonAsync<Tenant>() ?? tenant);
        return Result<Tenant>.Failure(await res.Content.ReadAsStringAsync());
    }

    public async Task<Result<Tenant>> UpdateAsync(Tenant tenant)
    {
        var res = await _http.PutAsJsonAsync($"/api/tenants/{tenant.TenantId}", tenant);
        return res.IsSuccessStatusCode
            ? Result<Tenant>.Success(tenant)
            : Result<Tenant>.Failure(await res.Content.ReadAsStringAsync());
    }
}
