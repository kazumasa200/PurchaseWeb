using Infra.Persistance.Entities;
using Infra.Repositories;
using PurchaseWeb.Api.Usecases.Products;
using ClientModels = PurchaseWeb.Client.Models;

namespace PurchaseWeb.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        // 公開: 客が /preorder/{tenantId} で商品を見るために必要なぶんだけ。
        // 認可つき: 店員だけが触れるマスタ操作と、削除済みも含む一覧。
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
        }).RequireAuthorization();

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

        // base64 のまま返す版。商品編集ダイアログが「既存の画像を持ち回る」のに使うので残す。
        // 表示だけなら下の photo を使うこと（base64 は 33% 太るうえブラウザがキャッシュできない）。
        group.MapGet("{id}/image", async (string id, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var image = await usecase.GetProductImageAsync(id);
            return Results.Ok(image);
        }).RequireAuthorization();

        // 表示用。<img src> から直接叩けるようにバイナリで返す。
        // img タグはヘッダを付けられないので、テナントはクエリでも受ける。
        group.MapGet("{id}/photo", async (string id, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);

            var base64 = await usecase.GetProductImageAsync(id);
            if (string.IsNullOrEmpty(base64)) return Results.NotFound();

            byte[] bytes;
            try { bytes = Convert.FromBase64String(base64); }
            catch (FormatException) { return Results.NotFound(); }

            // ETag は中身から作るので、画像を差し替えれば必ず変わる。
            // max-age は 1 時間。イベント中の同時アクセスはこれで十分吸収でき、
            // 画像を差し替えても 1 時間で勝手に追従する（immutable にすると差し替えが反映されない）。
            var etag = $"\"{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..16]}\"";
            ctx.Response.Headers.CacheControl = "public, max-age=3600";

            return Results.File(bytes, SniffContentType(bytes),
                entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue(etag));
        });

        group.MapPost("", async (ClientModels.Product dto, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var entity = Product.Create(null, dto.ProductName, dto.Price, dto.Misc, dto.StockQuantity);
            var ret = await usecase.CreateAsync(entity, dto.ImageBase64);
            if (!ret.IsSuccess) return Results.BadRequest(ret.ErrorMessage);
            return Results.Ok(ToDto(ret.Data!));
        }).RequireAuthorization();

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
        }).RequireAuthorization();

        group.MapDelete("{id}", async (string id, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            var ret = await usecase.DeleteAsync(id);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        }).RequireAuthorization();

        group.MapPost("{id}/reduce-stock", async (string id, HttpContext ctx, IProductUsecase usecase, ITenantProvider tenant) =>
        {
            SetTenant(ctx, tenant);
            if (!int.TryParse(ctx.Request.Query["quantity"], out var qty))
                return Results.BadRequest("quantity が不正です");
            var ret = await usecase.ReduceStockAsync(id, qty);
            return ret.IsSuccess ? Results.Ok() : Results.BadRequest(ret.ErrorMessage);
        }).RequireAuthorization();
    }

    private static void SetTenant(HttpContext ctx, ITenantProvider tenant)
    {
        // 通常はヘッダ。<img src> のようにヘッダを付けられない経路ではクエリを使う。
        var tenantId = ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantId))
            tenantId = ctx.Request.Query["tenantId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
            tenant.SetTenantId(tenantId);
    }

    /// <summary>先頭バイトから画像の種類を判定する（DB には生の base64 しか入っていないため）</summary>
    private static string SniffContentType(byte[] b)
    {
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
        if (b.Length >= 4 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46) return "image/gif";
        return "application/octet-stream";
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
