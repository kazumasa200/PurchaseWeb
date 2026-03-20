using Infra.Persistance.Entities;
using PurchaseWeb.Api.Usecases.Tenants;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenants");

        group.MapGet("", async (ITenantUsecase usecase) =>
            Results.Ok((await usecase.GetActiveTenantsAsync()).Select(ToDto)));

        group.MapGet("all", async (ITenantUsecase usecase) =>
            Results.Ok((await usecase.GetAllTenantsAsync()).Select(ToDto)));

        group.MapPost("", async (ClientModels.Tenant dto, ITenantUsecase usecase) =>
        {
            var entity = Tenant.Create(dto.TenantName);
            var ret = await usecase.CreateAsync(entity);
            return ret.IsSuccess
                ? Results.Ok(ToDto(ret.Data!))
                : Results.BadRequest(ret.ErrorMessage);
        });

        group.MapPut("{id}", async (string id, ClientModels.Tenant dto, ITenantUsecase usecase) =>
        {
            var entity = new Tenant
            {
                TenantId   = id,
                TenantName = dto.TenantName,
                CreateDate = dto.CreateDate,
                UpdateDate = DateTime.Now,
                DeleteFlag = dto.DeleteFlag
            };
            var ret = await usecase.UpdateAsync(entity);
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
