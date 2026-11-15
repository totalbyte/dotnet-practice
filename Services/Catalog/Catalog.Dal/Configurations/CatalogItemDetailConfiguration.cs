using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CatalogItemDetailConfiguration : IEntityTypeConfiguration<CatalogItemDetail>
{
    public void Configure(EntityTypeBuilder<CatalogItemDetail> builder)
    {
        builder.ToTable("CatalogItemDetails", t =>
        {
            t.HasCheckConstraint("CK_CatalogItemDetails_WarrantyMonths", "[WarrantyMonths] BETWEEN 0 AND 60");
            t.HasCheckConstraint(
                "CK_CatalogItemDetails_Duration",
                "[EstimatedDurationMinutes] IS NULL OR [EstimatedDurationMinutes] > 0");
            t.HasCheckConstraint(
                "CK_CatalogItemDetails_Specifications",
                "[Specifications] IS NULL OR ISJSON([Specifications]) = 1");
        });
        builder.HasKey(d => d.CatalogItemId);

        builder.Property(d => d.CatalogItemId).ValueGeneratedNever();
        builder.Property(d => d.Description).HasMaxLength(CatalogItemDetail.DescriptionMaxLength);
        builder.Property(d => d.WarrantyMonths).HasDefaultValue(0).ValueGeneratedNever();
        builder.Property(d => d.Specifications).HasColumnType("nvarchar(max)");

        builder.HasQueryFilter(d => !d.CatalogItem.IsDeleted);
    }
}
