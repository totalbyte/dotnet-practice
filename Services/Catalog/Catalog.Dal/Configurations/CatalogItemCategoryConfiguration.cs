using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CatalogItemCategoryConfiguration : IEntityTypeConfiguration<CatalogItemCategory>
{
    public void Configure(EntityTypeBuilder<CatalogItemCategory> builder)
    {
        builder.ToTable("CatalogItemCategories");
        builder.HasKey(ic => new { ic.CatalogItemId, ic.CategoryId });

        builder.Property(ic => ic.AddedAt).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(ic => ic.IsPrimary).HasDefaultValue(false).ValueGeneratedNever();

        builder.HasIndex(ic => new { ic.CategoryId, ic.AddedAt })
            .IsDescending(false, true);
        builder.HasIndex(ic => ic.CatalogItemId)
            .IsUnique()
            .IncludeProperties(ic => ic.CategoryId)
            .HasFilter("[IsPrimary] = 1")
            .HasDatabaseName("UX_CatalogItemCategories_CatalogItemId_Primary");

        builder.HasOne(ic => ic.Category)
            .WithMany(c => c.ItemCategories)
            .HasForeignKey(ic => ic.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(ic => !ic.CatalogItem.IsDeleted);
    }
}
