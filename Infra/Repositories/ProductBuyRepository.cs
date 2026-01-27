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
    /// <returns></returns>
    public Task<List<ProductBuy>> GetActiveProductBuy();
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
}
