using Infra.Persistance.Entities;
using Infra.Repositories;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenants");

        // GET /api/tenants  → 有効なテナント一覧
        group.MapGet("", async (ITenantRepository repo) =>
        {
            var tenants = await repo.GetActiveTenantsAsync();
            return Results.Ok(tenants.Select(ToDto));
        });

        // GET /api/tenants/all  → 全テナント
        group.MapGet("all", async (ITenantRepository repo) =>
        {
            var tenants = await repo.GetAllTenantsAsync();
            return Results.Ok(tenants.Select(ToDto));
        });

        // POST /api/tenants
        group.MapPost("", async (ClientModels.Tenant dto, ITenantRepository repo) =>
        {
            var entity = Tenant.Create(dto.TenantName);
            var ret = await repo.AddAsync(entity);
            return ret.IsSuccess
                ? Results.Ok(ToDto(ret.Data!))
                : Results.BadRequest(ret.ErrorMessage);
        });

        // PUT /api/tenants/{id}
        group.MapPut("{id}", async (string id, ClientModels.Tenant dto, ITenantRepository repo) =>
        {
            var entity = new Tenant
            {
                TenantId   = id,
                TenantName = dto.TenantName,
                CreateDate = dto.CreateDate,
                UpdateDate = DateTime.Now,
                DeleteFlag = dto.DeleteFlag
            };
            var ret = await repo.UpdateAsync(entity);
            return ret.IsSuccess ? Results.Ok(ToDto(ret.Data!)) : Results.BadRequest(ret.ErrorMessage);
        });
    }

    private static ClientModels.Tenant ToDto(Tenant t) => new()
    {
        TenantId   = t.TenantId,
        TenantName = t.TenantName,
        CreateDate = t.CreateDate,
        UpdateDate = t.UpdateDate,
        DeleteFlag = t.DeleteFlag
    };
}
