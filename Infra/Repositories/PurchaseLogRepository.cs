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

public class PurchaseLogRepository : BaseRepository, IPurchaseLogRepository
{
    public PurchaseLogRepository(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantProvider tenantProvider)
        : base(dbFactory, tenantProvider)
    {
    }

    public async Task<List<PurchaseLog>> GetPurchaseLogsAsync()
    {
        return await ExecuteInContextAsync(context =>
            context.PurchaseLog
            .Where(x => x.TenantId == CurrentTenantId && !x.DeleteFlag)
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
                    // テナントIDを設定
                    SetTenantId(purchaseLog);

                    // デバッグ用
                    Console.WriteLine($"PurchaseLog TenantId after SetTenantId: {purchaseLog.TenantId}");
                    Console.WriteLine($"CurrentTenantId from Provider: {CurrentTenantId}");

                    // 重複チェック（テナント内）
                    var exists = await context.PurchaseLog
                        .AnyAsync(p => p.TenantId == CurrentTenantId
                            && p.LogId == purchaseLog.LogId
                            && !p.DeleteFlag);

                    if (exists)
                        return Result<List<PurchaseLog>>.Failure("同じIDの記録が既に存在します");
                }

                // 挿入前に再度確認
                foreach (var log in purchaseLogs)
                {
                    Console.WriteLine($"Inserting: LogId={log.LogId}, TenantId={log.TenantId}, ProductId={log.ProductId}");
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
            Console.WriteLine($"Error: {ex.Message}");
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
