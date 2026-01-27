using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Persistance.Configuration;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenant");
        builder.HasKey(x => x.TenantId);

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.TenantName)
            .HasColumnName("tenant_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CreateDate)
            .HasColumnName("create_date")
            .IsRequired();

        builder.Property(x => x.UpdateDate)
            .HasColumnName("update_date");

        builder.Property(x => x.DeleteFlag)
            .HasColumnName("delete_flag")
            .IsRequired();
    }
}
