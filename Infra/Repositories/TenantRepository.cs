using Infra.Persistance;
using Infra.Persistance.Context;
using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public interface ITenantRepository
{
    /// <summary>
    /// アクティブなテナント一覧を取得する
    /// </summary>
    Task<List<Tenant>> GetActiveTenantsAsync();

    /// <summary>
    /// 全てのテナントを取得する
    /// </summary>
    Task<List<Tenant>> GetAllTenantsAsync();

    /// <summary>
    /// テナントを追加する
    /// </summary>
    Task<Result<Tenant>> AddAsync(Tenant tenant);

    /// <summary>
    /// テナントを更新する
    /// </summary>
    Task<Result<Tenant>> UpdateAsync(Tenant tenant);
}

public class TenantRepository : BaseRepository, ITenantRepository
{
    public TenantRepository(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantProvider tenantProvider)
        : base(dbFactory, tenantProvider)
    {
    }

    public async Task<List<Tenant>> GetActiveTenantsAsync()
    {
        return await ExecuteInContextAsync(context =>
            context.Tenant
                .Where(x => !x.DeleteFlag)
                .OrderBy(x => x.CreateDate)
                .ToListAsync());
    }

    public async Task<List<Tenant>> GetAllTenantsAsync()
    {
        return await ExecuteInContextAsync(context =>
            context.Tenant
                .OrderBy(x => x.CreateDate)
                .ToListAsync());
    }

    public async Task<Result<Tenant>> AddAsync(Tenant tenant)
    {
        try
        {
            var result = await ExecuteInTransactionAsync(async context =>
            {
                // 重複チェック
                var exists = await context.Tenant
                    .AnyAsync(t => t.TenantName == tenant.TenantName && !t.DeleteFlag);

                if (exists)
                    return Result<Tenant>.Failure("同じ名前のテナントが既に存在します");

                var entry = await context.Tenant.AddAsync(tenant);
                await context.SaveChangesAsync();

                return Result<Tenant>.Success(entry.Entity);
            });

            return result;
        }
        catch (Exception ex)
        {
            return Result<Tenant>.Failure(ex.Message);
        }
    }

    public async Task<Result<Tenant>> UpdateAsync(Tenant tenant)
    {
        try
        {
            var result = await ExecuteInTransactionAsync(async context =>
            {
                // 重複チェック
                var exists = await context.Tenant
                    .AnyAsync(t => t.TenantName == tenant.TenantName
                        && t.TenantId != tenant.TenantId
                        && !t.DeleteFlag);

                if (exists)
                    return Result<Tenant>.Failure("同じ名前のテナントが既に存在します");

                var entry = context.Tenant.Update(tenant);
                await context.SaveChangesAsync();

                return Result<Tenant>.Success(entry.Entity);
            });

            return result;
        }
        catch (Exception ex)
        {
            return Result<Tenant>.Failure(ex.Message);
        }
    }
}
