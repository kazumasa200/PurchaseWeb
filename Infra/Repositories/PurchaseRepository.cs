using Infra.Persistance;
using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public interface IPurchaseRepository
{
    /// <summary>
    /// 購入履歴の記録と在庫の減算を、ひとつのトランザクションで行う。
    /// </summary>
    Task<Result<List<PurchaseLog>>> PurchaseAsync(List<PurchaseLog> purchaseLogs);
}

/// <summary>
/// レジの購入確定。
///
/// 以前はクライアントが「履歴の追加」と「在庫の減算」を別々の HTTP で呼んでいたため、
/// 途中で失敗すると履歴だけ残って在庫が減らない状態になり得た。
/// ここで 1 トランザクションにまとめている。
///
/// 在庫がマイナスになることは許容する（かずくんの宣言した仕様）。
/// StockQuantity が null の商品は「無制限」なので減らさない。
/// </summary>
public class PurchaseRepository : BaseRepository, IPurchaseRepository
{
    public PurchaseRepository(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantProvider tenantProvider)
        : base(dbFactory, tenantProvider)
    {
    }

    public async Task<Result<List<PurchaseLog>>> PurchaseAsync(List<PurchaseLog> purchaseLogs)
    {
        if (purchaseLogs.Count == 0)
            return Result<List<PurchaseLog>>.Failure("購入対象がありません");

        try
        {
            return await ExecuteInTransactionAsync(async context =>
            {
                var tenantId = CurrentTenantId;
                foreach (var log in purchaseLogs)
                    SetTenantId(log);

                var logIds = purchaseLogs.Select(l => l.LogId).ToList();
                if (logIds.Distinct().Count() != logIds.Count)
                    return Result<List<PurchaseLog>>.Failure("同じIDの記録が重複しています");

                var duplicated = await context.PurchaseLog
                    .AnyAsync(p => p.TenantId == tenantId && logIds.Contains(p.LogId) && !p.DeleteFlag);
                if (duplicated)
                    return Result<List<PurchaseLog>>.Failure("同じIDの記録が既に存在します");

                // 同じ商品が複数行に分かれていても 1 回の問い合わせで足りるようにまとめて引く
                var productIds = purchaseLogs.Select(l => l.ProductId).Distinct().ToList();
                var products = await context.Product
                    .Where(p => p.TenantId == tenantId && productIds.Contains(p.ProductId) && !p.DeleteFlag)
                    .ToDictionaryAsync(p => p.ProductId);

                foreach (var log in purchaseLogs)
                {
                    if (!products.TryGetValue(log.ProductId, out var product))
                        return Result<List<PurchaseLog>>.Failure($"商品が見つかりません: {log.ProductId}");

                    // null は無制限。マイナスは許容する仕様なので下限は設けない
                    if (product.StockQuantity.HasValue)
                    {
                        product.StockQuantity -= log.Amount;
                        product.UpdateDate = DateTime.Now;
                    }
                }

                await context.PurchaseLog.AddRangeAsync(purchaseLogs);
                await context.SaveChangesAsync();
                return Result<List<PurchaseLog>>.Success(purchaseLogs);
            });
        }
        catch (Exception ex)
        {
            return Result<List<PurchaseLog>>.Failure($"購入処理エラー: {ex.Message}");
        }
    }
}
