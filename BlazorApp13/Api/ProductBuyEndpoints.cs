using Infra.Repositories;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api;

public static class ProductBuyEndpoints
{
    public static void MapProductBuyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/productbuy");

        // GET /api/productbuy （画像なし。画像は /api/products/{id}/image で別途取得）
        group.MapGet("", async (HttpContext ctx, IProductBuyRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var items = await repo.GetActiveProductBuyWithoutImages();
            return Results.Ok(items.Select(pb => new ClientModels.ProductBuy
            {
                Amount  = pb.Amount,
                Product = new ClientModels.Product
                {
                    ProductId     = pb.Product.ProductId,
                    ProductName   = pb.Product.ProductName,
                    Price         = pb.Product.Price,
                    Misc          = pb.Product.Misc,
                    ImageBase64   = null,
                    StockQuantity = pb.Product.StockQuantity,
                    CreateDate    = pb.Product.CreateDate,
                    UpdateDate    = pb.Product.UpdateDate,
                    DeleteFlag    = pb.Product.DeleteFlag,
                    TenantId      = pb.Product.TenantId
                }
            }));
        });
    }

    private static void SetTenant(HttpContext ctx, ITenantProvider tenant)
    {
        var tenantId = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
            tenant.SetTenantId(tenantId);
    }
}
