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
}

public class ProductRepository(IDbContextFactory<ApplicationDbContext> dbFactory) : BaseRepository(dbFactory), IProductRepository
{
    public async Task<List<Product>> GetActiveProducts()
    {
        return await ExecuteInContextAsync(context =>
            context.Product
                .Where(x => !x.DeleteFlag)
                .OrderBy(x => x.CreateDate)
                .ToListAsync());
    }

    public async Task<List<Product>> GetAllProducts()
    {
        try
        {
            return await ExecuteInContextAsync(context =>
            context.Product
                .OrderBy(x => x.CreateDate)
                .ToListAsync());
        }
        catch
        {
            return [];
        }
    }

    public async Task<Result<Product>> AddAsync(Product product)
    {
        try
        {
            var result = await ExecuteInTransactionAsync(async context =>
            {
                // 重複チェック
                var exists = await context.Product
                    .AnyAsync(p => p.ProductName == product.ProductName && !p.DeleteFlag);

                if (exists)
                    return Result<Product>.Failure("同じ名前の商品が既に存在します");

                // 商品の追加
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
                // 重複チェック
                var exists = await context.Product
                    .AnyAsync(p => p.ProductName == product.ProductName && p.ProductId != product.ProductId && !p.DeleteFlag);

                if (exists)
                    return Result<Product>.Failure("同じ名前の商品が既に存在します");

                // 商品の追加
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
}