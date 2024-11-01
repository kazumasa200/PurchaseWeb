using Infra.Persistance;
using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public interface IPurchaseLogRepository
{
    /// <summary>
    /// 購入履歴を取得する
    /// </summary>
    /// <returns></returns>
    public Task<List<PurchaseLog>> GetPurchaseLogsAsync();

    /// <summary>
    /// 購入履歴を追加する
    /// </summary>
    /// <param name="purchaseLog"></param>
    /// <returns></returns>
    public Task<Result<List<PurchaseLog>>> AddRangeAsync(List<PurchaseLog> purchaseLogs);

    /// <summary>
    /// 購入履歴を削除する
    /// </summary>
    /// <param name="purchaseLog"></param>
    /// <returns></returns>
    public Task<Result<PurchaseLog>> DeleteAsync(PurchaseLog purchaseLog);
}

public class PurchaseLogRepository(IDbContextFactory<ApplicationDbContext> dbFactory) : BaseRepository(dbFactory), IPurchaseLogRepository
{
    public async Task<List<PurchaseLog>> GetPurchaseLogsAsync()
    {
        return await ExecuteInContextAsync(context =>
            context.PurchaseLog
            .Where(x => x.DeleteFlag == false)
            .OrderByDescending(x => x.PurchaseDate)
            .ToListAsync()
        );
    }

    public async Task<Result<List<PurchaseLog>>> AddRangeAsync(List<PurchaseLog> purchaseLogs)
    {
        try
        {
            var result = await ExecuteInTransactionAsync(async context =>
            {
                foreach (var purchaseLog in purchaseLogs)
                {
                    // 重複チェック
                    var exists = await context.PurchaseLog
                        .AnyAsync(p => p.LogId == purchaseLog.LogId && !p.DeleteFlag);

                    if (exists)
                        return Result<List<PurchaseLog>>.Failure("同じIDの記録が既に存在します");
                }
                // 商品の追加
                await context.PurchaseLog.AddRangeAsync(purchaseLogs);
                await context.SaveChangesAsync();
                return Result<List<PurchaseLog>>.Success(purchaseLogs);
            });

            return result;
        }
        catch (Exception ex)
        {
            return Result<List<PurchaseLog>>.Failure(ex.Message);
        }
    }

    public async Task<Result<PurchaseLog>> DeleteAsync(PurchaseLog purchaseLog)
    {
        try
        {
            var result = await ExecuteInTransactionAsync(async context =>
            {
                // 重複チェック
                var exists = await context.PurchaseLog
                    .AnyAsync(p => p.LogId == purchaseLog.LogId && !p.DeleteFlag);

                if (!exists)
                    return Result<PurchaseLog>.Failure("購入情報が見つからないか削除済みです");

                // 商品の追加
                var entry = context.PurchaseLog.Update(purchaseLog);
                await context.SaveChangesAsync();

                return Result<PurchaseLog>.Success(entry.Entity);
            });

            return result;
        }
        catch (Exception ex)
        {
            return Result<PurchaseLog>.Failure(ex.Message);
        }
    }
}