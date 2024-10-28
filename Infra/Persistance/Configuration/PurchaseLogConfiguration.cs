using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Persistance.Configuration;

/// <summary>
/// 購入ログエンティティの設定クラス
/// </summary>
public class PurchaseLogConfiguration : IEntityTypeConfiguration<PurchaseLog>
{
    public void Configure(EntityTypeBuilder<PurchaseLog> builder)
    {
        builder.ToTable("purchase_log");

        // 主キーの設定
        builder.HasKey(e => e.LogId);

        // プロパティの設定
        builder.Property(e => e.LogId)
            .HasColumnName("log_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ProductId)
            .HasColumnName("product_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PurchaseDate)
            .HasColumnName("purchase_date")
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnName("amount")
            .IsRequired();

        builder.Property(e => e.DeleteFlag)
            .HasColumnName("delete_flag")
            .IsRequired();

        // リレーションシップの設定
        builder.HasOne(e => e.Product)
            .WithMany(p => p.PurchaseLogs)
            .HasForeignKey(e => e.ProductId)
            .HasConstraintName("purchase_log_product_fk");
    }
}