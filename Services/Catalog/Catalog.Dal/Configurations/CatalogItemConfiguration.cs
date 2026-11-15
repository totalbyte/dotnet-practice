using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("CatalogItems", t =>
        {
            t.HasCheckConstraint("CK_CatalogItems_ItemType", "[ItemType] IN (N'Part', N'Service')");
            t.HasCheckConstraint("CK_CatalogItems_Price", "[Price] > 0");
            t.HasCheckConstraint(
                "CK_CatalogItems_ServiceHasNoBrand", "[ItemType] = N'Part' OR [BrandId] IS NULL");
        });
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Sku).IsRequired().HasMaxLength(CatalogItem.SkuMaxLength);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(CatalogItem.NameMaxLength);
        builder.Property(i => i.ItemType).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(i => i.Price).HasColumnType("decimal(18,2)");
        builder.Property(i => i.IsActive).HasDefaultValue(true).ValueGeneratedNever();
        builder.Property(i => i.CreatedAt).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(i => i.CreatedBy).HasMaxLength(CatalogItem.AuthorMaxLength);
        builder.Property(i => i.UpdatedAt).HasPrecision(0);
        builder.Property(i => i.UpdatedBy).HasMaxLength(CatalogItem.AuthorMaxLength);
        builder.Property(i => i.IsDeleted).HasDefaultValue(false).ValueGeneratedNever();
        builder.Property(i => i.RowVersion).IsRowVersion();

        builder.HasIndex(i => i.Sku).IsUnique().HasDatabaseName("UQ_CatalogItems_Sku");
        builder.HasIndex(i => i.Name)
            .IncludeProperties(i => new { i.Sku, i.Price, i.ItemType })
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(i => new { i.ItemType, i.Price })
            .IncludeProperties(i => new { i.Name, i.Sku })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");
        builder.HasIndex(i => i.BrandId)
            .HasFilter("[BrandId] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasOne(i => i.Brand)
            .WithMany(b => b.Items)
            .HasForeignKey(i => i.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Detail)
            .WithOne(d => d.CatalogItem)
            .HasForeignKey<CatalogItemDetail>(d => d.CatalogItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Categories)
            .WithOne(c => c.CatalogItem)
            .HasForeignKey(c => c.CatalogItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(i => !i.IsDeleted);
    }
}
