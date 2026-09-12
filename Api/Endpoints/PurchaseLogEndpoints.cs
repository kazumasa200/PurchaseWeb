using Infra.Persistance.Entities;
using Infra.Repositories;
using PurchaseWeb.Api.Usecases.PurchaseLogs;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api.Endpoints;

public static class PurchaseLogEndpoints
{
    public static void MapPurchaseLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/purchaselogs");

        group.MapGet("", async (HttpContext ctx, IPurchaseLogUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var logs = await usecase.GetPurchaseLogsAsync();
            return Results.Ok(logs.Select(ToDto));
        }).RequireAuthorization();

        group.MapPost("", async (List<ClientModels.PurchaseLog> dtos, HttpContext ctx,
            IPurchaseLogUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var entities = dtos.Select(d => PurchaseLog.Create(d.Amount, d.ProductId, null)).ToList();
            var ret = await usecase.AddRangeAsync(entities);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        }).RequireAuthorization();

        group.MapDelete("{id}", async (string id, HttpContext ctx,
            IPurchaseLogUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var ret = await usecase.DeleteAsync(id);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        }).RequireAuthorization();
    }

    private static void SetTenant(HttpContext ctx, ITenantProvider tenant)
    {
        var tenantId = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
            tenant.SetTenantId(tenantId);
    }

    private static ClientModels.PurchaseLog ToDto(PurchaseLog l) => new()
    {
        LogId        = l.LogId,
        ProductId    = l.ProductId,
        Amount       = l.Amount,
        PurchaseDate = l.PurchaseDate,
        DeleteFlag   = l.DeleteFlag,
        TenantId     = l.TenantId
    };
}
