using Infra.Persistance;
using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public interface IProductBuyRepository
{
    /// <summary>
    /// 商品一覧を取得する
    /// </summary>
    public Task<List<ProductBuy>> GetActiveProductBuy();

    /// <summary>
    /// 商品一覧を画像なしで取得する（高速）
    /// </summary>
    public Task<List<ProductBuy>> GetActiveProductBuyWithoutImages();
}

public class ProductBuyRepository : BaseRepository, IProductBuyRepository
{
    public ProductBuyRepository(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantProvider tenantProvider)
        : base(dbFactory, tenantProvider)
    {
    }

    public async Task<List<ProductBuy>> GetActiveProductBuy()
    {
        return await ExecuteInContextAsync(async context =>
        {
            var products = await context.Product
                .Where(x => x.TenantId == CurrentTenantId && !x.DeleteFlag)
                .OrderBy(x => x.CreateDate)
                .ToListAsync();

            return products.Select(ProductBuy.CreateFromProduct).ToList();
        });
    }

    public async Task<List<ProductBuy>> GetActiveProductBuyWithoutImages()
    {
        return await ExecuteInContextAsync(async context =>
        {
            var products = await context.Product
                .Where(x => x.TenantId == CurrentTenantId && !x.DeleteFlag)
                .OrderBy(x => x.CreateDate)
                .Select(p => new Product
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Misc = p.Misc,
                    StockQuantity = p.StockQuantity,
                    CreateDate = p.CreateDate,
                    UpdateDate = p.UpdateDate,
                    DeleteFlag = p.DeleteFlag,
                    TenantId = p.TenantId
                    // ImageBase64 は除外（null のまま）
                })
                .ToListAsync();

            return products.Select(ProductBuy.CreateFromProduct).ToList();
        });
    }
}
