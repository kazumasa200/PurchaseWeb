using Infra.Persistance.Entities;
using Infra.Repositories;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api;

public static class PurchaseLogEndpoints
{
    public static void MapPurchaseLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/purchaselogs");

        // GET /api/purchaselogs
        group.MapGet("", async (HttpContext ctx, IPurchaseLogRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var logs = await repo.GetPurchaseLogsAsync();
            return Results.Ok(logs.Select(ToDto));
        });

        // POST /api/purchaselogs  → 複数ログ追加
        group.MapPost("", async (List<ClientModels.PurchaseLog> dtos, HttpContext ctx,
            IPurchaseLogRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var entities = dtos.Select(d => PurchaseLog.Create(d.Amount, d.ProductId, null)).ToList();
            var ret = await repo.AddRangeAsync(entities);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        });

        // DELETE /api/purchaselogs/{id}
        group.MapDelete("{id}", async (string id, HttpContext ctx,
            IPurchaseLogRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var logs = await repo.GetPurchaseLogsAsync();
            var target = logs.FirstOrDefault(l => l.LogId == id);
            if (target == null) return Results.NotFound();
            var deleted = PurchaseLog.Delete(target);
            var ret = await repo.DeleteAsync(deleted);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        });
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
