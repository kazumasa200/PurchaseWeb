using Infra.Persistance.Entities;
using Infra.Repositories;
using PurchaseWeb.Api.Usecases.Purchases;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api.Endpoints;

public static class PurchaseEndpoints
{
    public static void MapPurchaseEndpoints(this WebApplication app)
    {
        // レジの購入確定。履歴の追加と在庫の減算を 1 リクエスト・1 トランザクションで行う。
        app.MapPost("/api/purchase", async (List<ClientModels.PurchaseLog> dtos, HttpContext ctx,
            IPurchaseUsecase usecase, ITenantProvider tenant) =>
        {
            var tenantId = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrEmpty(tenantId))
                tenant.SetTenantId(tenantId);

            List<PurchaseLog> entities;
            try
            {
                entities = dtos.Select(d => PurchaseLog.Create(d.Amount, d.ProductId, null)).ToList();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }

            var ret = await usecase.PurchaseAsync(entities);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        }).RequireAuthorization();
    }
}
