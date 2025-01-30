using Infra.Persistance.Context;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistance;

/// <summary>
/// リポジトリの基底クラス
/// DbContextの生成と破棄を管理する
/// </summary>
/// <remarks>
/// コンストラクタ
/// </remarks>
/// <param name="dbFactory">DbContextファクトリ</param>
public abstract class BaseRepository(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory = dbFactory;

    /// <summary>
    /// DbContextを使用して非同期操作を実行する
    /// </summary>
    /// <typeparam name="T">戻り値の型</typeparam>
    /// <param name="operation">実行する操作</param>
    /// <returns>操作の結果</returns>
    protected async Task<T> ExecuteInContextAsync<T>(Func<ApplicationDbContext, Task<T>> operation)
    {
        using var context = _dbFactory.CreateDbContext();
        return await operation(context);
    }

    /// <summary>
    /// DbContextを使用して同期操作を実行する
    /// </summary>
    /// <typeparam name="T">戻り値の型</typeparam>
    /// <param name="operation">実行する操作</param>
    /// <returns>操作の結果</returns>
    protected T ExecuteInContext<T>(Func<ApplicationDbContext, T> operation)
    {
        using var context = _dbFactory.CreateDbContext();
        return operation(context);
    }

    /// <summary>
    /// トランザクション内で非同期操作を実行する
    /// 例外発生時は自動的にロールバックされる
    /// </summary>
    /// <typeparam name="T">戻り値の型</typeparam>
    /// <param name="operation">実行する操作</param>
    /// <returns>操作の結果</returns>
    protected async Task<T> ExecuteInTransactionAsync<T>(Func<ApplicationDbContext, Task<T>> operation)
    {
        using var context = _dbFactory.CreateDbContext();
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation(context);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
