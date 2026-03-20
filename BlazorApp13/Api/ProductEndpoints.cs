using Infra.Persistance.Entities;
using Infra.Repositories;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products");

        // GET /api/products  （画像なし一覧）
        group.MapGet("", async (HttpContext ctx, IProductRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var products = await repo.GetActiveProductsWithoutImages();
            return Results.Ok(products.Select(ToDto));
        });

        // GET /api/products/all → 削除済み含む全商品
        group.MapGet("all", async (HttpContext ctx, IProductRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var products = await repo.GetAllProducts();
            return Results.Ok(products.Select(ToDto));
        });

        // GET /api/products/{id}
        group.MapGet("{id}", async (string id, HttpContext ctx, IProductRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var p = await repo.GetByIdAsync(id);
            return p == null ? Results.NotFound() : Results.Ok(ToDto(p));
        });

        // GET /api/products/{id}/image → 画像のみ取得（クライアント側でキャッシュ）
        group.MapGet("{id}/image", async (string id, HttpContext ctx, IProductRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var image = await repo.GetProductImageAsync(id);
            return Results.Ok(image);
        });

        // POST /api/products
        group.MapPost("", async (ClientModels.Product dto, HttpContext ctx, IProductRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var entity = Product.Create(null, dto.ProductName, dto.Price, dto.Misc, dto.StockQuantity);
            var ret = await repo.AddAsync(entity);
            if (!ret.IsSuccess) return Results.BadRequest(ret.ErrorMessage);

            // 画像を ProductImages テーブルに保存
            if (!string.IsNullOrEmpty(dto.ImageBase64))
                await repo.SaveImageAsync(ret.Data!.ProductId, dto.ImageBase64);

            return Results.Ok(ToDto(ret.Data!));
        });

        // PUT /api/products/{id}
        group.MapPut("{id}", async (string id, ClientModels.Product dto, HttpContext ctx, IProductRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var existing = await repo.GetByIdAsync(id);
            if (existing == null) return Results.NotFound();

            existing.ProductName   = dto.ProductName;
            existing.Price         = dto.Price;
            existing.Misc          = dto.Misc;
            existing.StockQuantity = dto.StockQuantity;

            var updated = Product.Update(existing);
            var ret = await repo.UpdateAsync(updated);
            if (!ret.IsSuccess) return Results.BadRequest(ret.ErrorMessage);

            // 画像が送られてきた場合のみ更新（null = 変更なし、空文字 = 削除）
            if (dto.ImageBase64 != null)
                await repo.SaveImageAsync(id, dto.ImageBase64);

            return Results.Ok();
        });

        // DELETE /api/products/{id}
        group.MapDelete("{id}", async (string id, HttpContext ctx, IProductRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var existing = await repo.GetByIdAsync(id);
            if (existing == null) return Results.NotFound();
            var deleted = Product.Delete(existing);
            var ret = await repo.UpdateAsync(deleted);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        });

        // POST /api/products/{id}/reduce-stock?quantity=N
        group.MapPost("{id}/reduce-stock", async (string id, HttpContext ctx, IProductRepository repo, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            if (!int.TryParse(ctx.Request.Query["quantity"], out var qty))
                return Results.BadRequest("quantity が不正です");
            var ret = await repo.ReduceStockAsync(id, qty);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        });
    }

    private static void SetTenant(HttpContext ctx, ITenantProvider tenant)
    {
        var tenantId = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
            tenant.SetTenantId(tenantId);
    }

    private static ClientModels.Product ToDto(Product p) => new()
    {
        ProductId     = p.ProductId,
        ProductName   = p.ProductName,
        Price         = p.Price,
        Misc          = p.Misc,
        ImageBase64   = null, // 画像は /api/products/{id}/image で別途取得
        StockQuantity = p.StockQuantity,
        CreateDate    = p.CreateDate,
        UpdateDate    = p.UpdateDate,
        DeleteFlag    = p.DeleteFlag,
        TenantId      = p.TenantId
    };
}
