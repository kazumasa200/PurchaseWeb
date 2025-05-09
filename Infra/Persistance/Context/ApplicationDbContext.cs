using Infra.Persistance.Configuration;
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

    public DbSet<Product> Product { get; set; }

    public DbSet<PurchaseLog> PurchaseLog { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseLogConfiguration());
    }
}