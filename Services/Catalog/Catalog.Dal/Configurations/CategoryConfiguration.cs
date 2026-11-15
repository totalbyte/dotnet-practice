using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Dal.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", t =>
        {
            t.HasCheckConstraint("CK_Categories_Slug", "LEN([Slug]) > 0 AND [Slug] NOT LIKE N'% %'");
            t.HasCheckConstraint("CK_Categories_DisplayOrder", "[DisplayOrder] >= 0");
            t.HasCheckConstraint(
                "CK_Categories_NotOwnParent", "[ParentCategoryId] IS NULL OR [ParentCategoryId] <> [Id]");
        });
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(Category.NameMaxLength);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(Category.SlugMaxLength);
        builder.Property(c => c.DisplayOrder).HasDefaultValue(0).ValueGeneratedNever();
        builder.Property(c => c.CreatedAt).HasPrecision(0).HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(c => c.Name).IsUnique().HasDatabaseName("UQ_Categories_Name");
        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("UQ_Categories_Slug");
        builder.HasIndex(c => new { c.ParentCategoryId, c.DisplayOrder })
            .IncludeProperties(c => new { c.Name, c.Slug });

        builder.HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            Seed(1, "Запчастини", "parts", null, 1),
            Seed(2, "Послуги", "services", null, 2),
            Seed(3, "Екрани та дисплеї", "displays", 1, 1),
            Seed(4, "Акумулятори", "batteries", 1, 2),
            Seed(5, "Накопичувачі", "storage", 1, 3),
            Seed(6, "Витратні матеріали", "consumables", 1, 4),
            Seed(7, "Для смартфонів", "for-smartphones", 1, 5),
            Seed(8, "Для ноутбуків", "for-laptops", 1, 6),
            Seed(9, "Діагностика", "diagnostics", 2, 1),
            Seed(10, "Ремонт смартфонів", "smartphone-repair", 2, 2),
            Seed(11, "Ремонт ноутбуків", "laptop-repair", 2, 3));
    }

    private static object Seed(int id, string name, string slug, int? parentCategoryId, int displayOrder) => new
    {
        Id = id,
        Name = name,
        Slug = slug,
        ParentCategoryId = parentCategoryId,
        DisplayOrder = displayOrder,
        SeedData.CreatedAt
    };
}
