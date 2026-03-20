using Infra.Persistance;
using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public interface IProductRepository
{
    Task<List<Product>> GetActiveProducts();
    Task<List<Product>> GetActiveProductsWithoutImages();
    Task<string?> GetProductImageAsync(string productId);
    Task<Dictionary<string, string?>> GetAllProductImagesAsync();
    Task<List<Product>> GetAllProducts();
    Task<Result<Product>> AddAsync(Product product);
    Task<Result<Product>> UpdateAsync(Product product);
    Task<Product?> GetByIdAsync(string productId);
    Task<Result<Product>> ReduceStockAsync(string productId, int quantity);

    /// <summary>購入取り消し時に在庫を戻す（null / -1 = 無制限は対象外）</summary>
    Task RestoreStockAsync(string productId, int quantity);

    /// <summary>商品画像を保存（null または空文字は削除）</summary>
    Task SaveImageAsync(string productId, string? imageBase64);
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
                .Where(x => x.TenantId == CurrentTenantId && !x.DeleteFlag)
                .OrderBy(x => x.CreateDate)
                .ToListAsync());
    }

    public async Task<List<Product>> GetActiveProductsWithoutImages()
    {
        // 画像は別テーブルなので同じクエリで OK（Product に ImageBase64 なし）
        return await GetActiveProducts();
    }

    public async Task<string?> GetProductImageAsync(string productId)
    {
        return await ExecuteInContextAsync(context =>
            context.ProductImages
                .Where(pi => pi.ProductId == productId
                    && pi.Product!.TenantId == CurrentTenantId
                    && !pi.Product.DeleteFlag)
                .Select(pi => pi.ImageBase64)
                .FirstOrDefaultAsync());
    }

    public async Task<Dictionary<string, string?>> GetAllProductImagesAsync()
    {
        return await ExecuteInContextAsync(async context =>
        {
            var images = await context.ProductImages
                .Where(pi => pi.Product!.TenantId == CurrentTenantId && !pi.Product.DeleteFlag)
                .Select(pi => new { pi.ProductId, pi.ImageBase64 })
                .ToListAsync();
            return images.ToDictionary(x => x.ProductId, x => x.ImageBase64);
        });
    }

    public async Task SaveImageAsync(string productId, string? imageBase64)
    {
        await ExecuteInTransactionAsync<int>(async context =>
        {
            var existing = await context.ProductImages
                .FirstOrDefaultAsync(pi => pi.ProductId == productId);

            if (existing != null)
            {
                if (string.IsNullOrEmpty(imageBase64))
                    context.ProductImages.Remove(existing);
                else
                    existing.ImageBase64 = imageBase64;
            }
            else if (!string.IsNullOrEmpty(imageBase64))
            {
                await context.ProductImages.AddAsync(new ProductImage
                {
                    ImageId     = Guid.NewGuid().ToString(),
                    ProductId   = productId,
                    ImageBase64 = imageBase64,
                    CreatedAt   = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
            return 0;
        });
    }

    public async Task<List<Product>> GetAllProducts()
    {
        try
        {
            return await ExecuteInContextAsync(context =>
                context.Product
                    .Where(x => x.TenantId == CurrentTenantId)
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
            return await ExecuteInTransactionAsync(async context =>
            {
                SetTenantId(product);

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
            return await ExecuteInTransactionAsync(async context =>
            {
                SetTenantId(product);

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
        }
        catch (Exception ex)
        {
            return Result<Product>.Failure(ex.Message);
        }
    }

    public async Task<Result<Product>> ReduceStockAsync(string productId, int quantity)
    {
        try
        {
            return await ExecuteInTransactionAsync(async context =>
            {
                var product = await context.Product
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ProductId == productId
                        && p.TenantId == CurrentTenantId
                        && !p.DeleteFlag);

                if (product == null)
                    return Result<Product>.Failure("商品が見つかりません");

                // null は無制限扱い → 在庫変更なし
                if (!product.StockQuantity.HasValue)
                    return Result<Product>.Success(product);

                var updated = Product.ReduceStock(product, quantity);
                context.Product.Update(updated);
                await context.SaveChangesAsync();
                return Result<Product>.Success(updated);
            });
        }
        catch (Exception ex)
        {
            return Result<Product>.Failure($"在庫更新エラー: {ex.Message}");
        }
    }

    public async Task RestoreStockAsync(string productId, int quantity)
    {
        try
        {
            await ExecuteInTransactionAsync<int>(async context =>
            {
                var product = await context.Product
                    .FirstOrDefaultAsync(p => p.ProductId == productId
                        && p.TenantId == CurrentTenantId
                        && !p.DeleteFlag);

                // 商品が見つからない、null（無制限）は対象外
                if (product == null || !product.StockQuantity.HasValue)
                    return 0;

                product.StockQuantity += quantity;
                product.UpdateDate = DateTime.Now;
                await context.SaveChangesAsync();
                return 0;
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"RestoreStockAsync error: {ex.Message}");
        }
    }
}
