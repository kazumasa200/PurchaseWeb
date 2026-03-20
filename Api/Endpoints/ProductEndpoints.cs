using Infra.Persistance.Entities;
using Infra.Repositories;
using PurchaseWeb.Api.Usecases.Products;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/products");

        group.MapGet("", async (HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var products = await usecase.GetActiveProductsWithoutImagesAsync();
            return Results.Ok(products.Select(ToDto));
        });

        group.MapGet("all", async (HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var products = await usecase.GetAllProductsAsync();
            return Results.Ok(products.Select(ToDto));
        });

        group.MapGet("{id}", async (string id, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var p = await usecase.GetByIdAsync(id);
            return p == null ? Results.NotFound() : Results.Ok(ToDto(p));
        });

        group.MapGet("images", async (HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var images = await usecase.GetAllProductImagesAsync();
            return Results.Ok(images);
        });

        group.MapGet("{id}/image", async (string id, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var image = await usecase.GetProductImageAsync(id);
            return Results.Ok(image);
        });

        group.MapPost("", async (ClientModels.Product dto, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var entity = Product.Create(null, dto.ProductName, dto.Price, dto.Misc, dto.StockQuantity);
            var ret = await usecase.CreateAsync(entity, dto.ImageBase64);
            if (!ret.IsSuccess) return Results.BadRequest(ret.ErrorMessage);
            return Results.Ok(ToDto(ret.Data!));
        });

        group.MapPut("{id}", async (string id, ClientModels.Product dto, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var existing = await usecase.GetByIdAsync(id);
            if (existing == null) return Results.NotFound();

            existing.ProductName   = dto.ProductName;
            existing.Price         = dto.Price;
            existing.Misc          = dto.Misc;
            existing.StockQuantity = dto.StockQuantity;

            var updated = Product.Update(existing);
            var ret = await usecase.UpdateAsync(updated, dto.ImageBase64);
            if (!ret.IsSuccess) return Results.BadRequest(ret.ErrorMessage);
            return Results.Ok();
        });

        group.MapDelete("{id}", async (string id, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var ret = await usecase.DeleteAsync(id);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        });

        group.MapPost("{id}/reduce-stock", async (string id, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            if (!int.TryParse(ctx.Request.Query["quantity"], out var qty))
                return Results.BadRequest("quantity が不正です");
            var ret = await usecase.ReduceStockAsync(id, qty);
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
        ImageBase64   = null,
        StockQuantity = p.StockQuantity,
        CreateDate    = p.CreateDate,
        UpdateDate    = p.UpdateDate,
        DeleteFlag    = p.DeleteFlag,
        TenantId      = p.TenantId
    };
}
