using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistance.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public DbSet<Tenant> Tenant { get; set; }
    public DbSet<Product> Product { get; set; }
    public DbSet<PurchaseLog> PurchaseLog { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // すべてのConfigurationを自動適用
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
