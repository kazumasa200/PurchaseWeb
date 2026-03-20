using Infra.Persistance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infra.Persistance.Configuration;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");

        builder.HasKey(e => e.ImageId);

        builder.Property(e => e.ImageId)
            .HasColumnName("image_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ProductId)
            .HasColumnName("product_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ImageBase64)
            .HasColumnName("image_base64")
            .HasColumnType("TEXT");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(e => e.Product)
            .WithOne()
            .HasForeignKey<ProductImage>(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ProductId)
            .HasDatabaseName("ix_product_images_product_id");
    }
}
