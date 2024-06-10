using Microsoft.EntityFrameworkCore;

namespace BlazorApp13.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
         : base(options) {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public DbSet<Product> Product { get; set; }

    public DbSet<PurchaseLog> PurchaseLog { get; set; }
}
