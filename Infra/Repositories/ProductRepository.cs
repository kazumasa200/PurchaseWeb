using Infra.Persistance;
using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public interface IProductRepository
{
    /// <summary>
    /// 商品一覧を取得する
    /// </summary>
    /// <returns></returns>
    public Task<List<Product>> GetActiveProducts();

    /// <summary>
    /// 削除状態にかかわらず全ての商品を取得する
    /// </summary>
    /// <returns></returns>
    public Task<List<Product>> GetAllProducts();

    /// <summary>
    /// 商品を追加
    /// </summary>
    /// <param name="product">追加する商品</param>
    /// <returns>処理結果</returns>
    public Task<Result<Product>> AddAsync(Product product);

    /// <summary>
    /// 商品を更新
    /// </summary>
    /// <param name="product">追加する商品</param>
    /// <returns>処理結果</returns>
    public Task<Result<Product>> UpdateAsync(Product product);

    /// <summary>
    /// 商品IDで取得
    /// </summary>
    public Task<Product?> GetByIdAsync(string productId);

    /// <summary>
    /// 在庫を減らす
    /// </summary>
    public Task<Result<Product>> ReduceStockAsync(string productId, int quantity);
}

public class ProductRepository : BaseRepository, IProductRepository
{
    public ProductRepository(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantProvider tenantProvider)
        : base(dbFactory, tenantProvider)
    {
    }

    public async Task<List<Product>> GetActiveProducts()
    {
        return await ExecuteInContextAsync(context =>
            context.Product
                .Where(x => x.TenantId == CurrentTenantId && !x.DeleteFlag)  // テナントフィルタ追加
                .OrderBy(x => x.CreateDate)
                .ToListAsync());
    }

    public async Task<List<Product>> GetAllProducts()
    {
        try
        {
            return await ExecuteInContextAsync(context =>
                context.Product
                    .Where(x => x.TenantId == CurrentTenantId)  // テナントフィルタ追加
                    .OrderBy(x => x.CreateDate)
                    .ToListAsync());
        }
        catch
        {
            return [];
        }
    }

    public async Task<Product?> GetByIdAsync(string productId)
    {
        return await ExecuteInContextAsync(context =>
            context.Product
                .FirstOrDefaultAsync(p => p.ProductId == productId
                    && p.TenantId == CurrentTenantId
                    && !p.DeleteFlag));
    }

    public async Task<Result<Product>> AddAsync(Product product)
    {
        try
        {
            var result = await ExecuteInTransactionAsync(async context =>
            {
                // テナントIDを設定
                SetTenantId(product);

                // 重複チェック（テナント内）
                var exists = await context.Product
                    .AnyAsync(p => p.TenantId == CurrentTenantId
                        && p.ProductName == product.ProductName
                        && !p.DeleteFlag);

                if (exists)
                    return Result<Product>.Failure("同じ名前の商品が既に存在します");

                var entry = await context.Product.AddAsync(product);
                await context.SaveChangesAsync();

                return Result<Product>.Success(entry.Entity);
            });

            return result;
        }
        catch (Exception ex)
        {
            return Result<Product>.Failure(ex.Message);
        }
    }

    public async Task<Result<Product>> UpdateAsync(Product product)
    {
        try
        {
            var result = await ExecuteInTransactionAsync(async context =>
            {
                // テナントIDを設定
                SetTenantId(product);

                // 重複チェック（テナント内）
                var exists = await context.Product
                    .AnyAsync(p => p.TenantId == CurrentTenantId
                        && p.ProductName == product.ProductName
                        && p.ProductId != product.ProductId
                        && !p.DeleteFlag);

                if (exists)
                    return Result<Product>.Failure("同じ名前の商品が既に存在します");

                var entry = context.Product.Update(product);
                await context.SaveChangesAsync();

                return Result<Product>.Success(entry.Entity);
            });

            return result;
        }
        catch (Exception ex)
        {
            return Result<Product>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// 在庫を減らす
    /// </summary>
    public async Task<Result<Product>> ReduceStockAsync(string productId, int quantity)
    {
        try
        {
            var result = await ExecuteInTransactionAsync(async context =>
            {
                var product = await context.Product
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductId == productId
                        && p.TenantId == CurrentTenantId
                        && !p.DeleteFlag);

                if (product == null)
                    return Result<Product>.Failure("商品が見つかりません");

                if (product.StockQuantity.HasValue)
                {
                    var updated = Product.ReduceStock(product, quantity);
                    context.Product.Update(updated);
                    await context.SaveChangesAsync();
                    return Result<Product>.Success(updated);
                }

                return Result<Product>.Success(product);
            });

            return result;
        }
        catch (Exception ex)
        {
            return Result<Product>.Failure($"在庫更新エラー: {ex.Message}");
        }
    }
}
