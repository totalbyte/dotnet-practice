using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class Category
{
    public const int NameMaxLength = 100;
    public const int SlugMaxLength = 100;

    private readonly List<Category> _children = [];
    private readonly List<CatalogItemCategory> _itemCategories = [];

    private Category()
    {
    }

    public Category(string name, string slug, int? parentCategoryId = null, int displayOrder = 0)
    {
        Rename(name);
        ChangeSlug(slug);
        MoveTo(parentCategoryId);
        ChangeDisplayOrder(displayOrder);
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public int? ParentCategoryId { get; private set; }
    public Category? Parent { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyCollection<Category> Children => _children;
    public IReadOnlyCollection<CatalogItemCategory> ItemCategories => _itemCategories;

    public void Rename(string name) => Name = Guard.Required(name, NameMaxLength, "Category name");

    public void ChangeSlug(string slug)
    {
        var value = Guard.Required(slug, SlugMaxLength, "Category slug");
        if (value.Any(char.IsWhiteSpace))
            throw new DomainException("Category slug must not contain whitespace");
        Slug = value;
    }

    public void MoveTo(int? parentCategoryId)
    {
        if (parentCategoryId is not null && Id != 0 && parentCategoryId == Id)
            throw new DomainException("Category cannot be its own parent");
        ParentCategoryId = parentCategoryId;
    }

    public void ChangeDisplayOrder(int displayOrder)
    {
        if (displayOrder < 0)
            throw new DomainException("Display order must not be negative");
        DisplayOrder = displayOrder;
    }
}
