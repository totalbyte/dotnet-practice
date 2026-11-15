using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name).IsRequired().HasMaxLength(Brand.NameMaxLength);
        builder.Property(b => b.Country).HasMaxLength(Brand.CountryMaxLength);
        builder.Property(b => b.CreatedAt).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(b => b.Name).IsUnique().HasDatabaseName("UQ_Brands_Name");

        builder.HasData(
            new { Id = 1, Name = "Apple", Country = "США", SeedData.CreatedAt },
            new { Id = 2, Name = "Samsung", Country = "Південна Корея", SeedData.CreatedAt },
            new { Id = 3, Name = "Kingston", Country = "США", SeedData.CreatedAt },
            new { Id = 4, Name = "Arctic", Country = "Швейцарія", SeedData.CreatedAt },
            new { Id = 5, Name = "Lenovo", Country = "Китай", SeedData.CreatedAt });
    }
}
