using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Persistance.Configuration;

/// <summary>
/// 商品エンティティの設定クラス
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("product");

        // 主キーの設定
        builder.HasKey(e => e.ProductId);

        // プロパティの設定
        builder.Property(e => e.ProductId)
            .HasColumnName("product_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ProductName)
            .HasColumnName("product_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Price)
            .HasColumnName("price")
            .IsRequired();

        builder.Property(e => e.CreateDate)
            .HasColumnName("create_date")
            .IsRequired();

        builder.Property(e => e.Misc)
            .HasColumnName("misc")
            .HasMaxLength(100);

        builder.Property(e => e.UpdateDate)
            .HasColumnName("update_date");

        builder.Property(e => e.DeleteFlag)
            .HasColumnName("delete_flag")
            .IsRequired();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
