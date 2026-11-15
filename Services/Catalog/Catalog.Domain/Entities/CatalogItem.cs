using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Entities;

public class CatalogItem
{
    public const int SkuMaxLength = 50;
    public const int NameMaxLength = 200;
    public const int AuthorMaxLength = 100;

    private readonly List<CatalogItemCategory> _categories = [];

    private CatalogItem()
    {
    }

    public CatalogItem(
        string sku, string name, CatalogItemType itemType, decimal price,
        int? brandId = null, string? createdBy = null)
    {
        Sku = Guard.Required(sku, SkuMaxLength, "SKU");
        Name = Guard.Required(name, NameMaxLength, "Item name");
        ItemType = itemType;
        Price = ValidPrice(price);
        BrandId = ValidBrand(brandId);
        CreatedBy = Guard.Optional(createdBy, AuthorMaxLength, "Author");
        IsActive = true;
    }

    public long Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public CatalogItemType ItemType { get; private set; }
    public decimal Price { get; private set; }
    public int? BrandId { get; private set; }
    public Brand? Brand { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public CatalogItemDetail? Detail { get; private set; }
    public IReadOnlyCollection<CatalogItemCategory> Categories => _categories;

    public void Rename(string name, string? updatedBy = null)
    {
        Name = Guard.Required(name, NameMaxLength, "Item name");
        Touch(updatedBy);
    }

    public void ChangePrice(decimal price, string? updatedBy = null)
    {
        Price = ValidPrice(price);
        Touch(updatedBy);
    }

    public void AssignBrand(int? brandId, string? updatedBy = null)
    {
        BrandId = ValidBrand(brandId);
        Touch(updatedBy);
    }

    public void Activate(string? updatedBy = null)
    {
        if (IsDeleted)
            throw new DomainException("Deleted item cannot be activated");
        IsActive = true;
        Touch(updatedBy);
    }

    public void Deactivate(string? updatedBy = null)
    {
        IsActive = false;
        Touch(updatedBy);
    }

    public void Describe(
        string? description, int warrantyMonths, int? estimatedDurationMinutes, string? specifications,
        string? updatedBy = null)
    {
        if (estimatedDurationMinutes is not null && ItemType != CatalogItemType.Service)
            throw new DomainException("Estimated duration applies to services only");

        if (Detail is null)
            Detail = new CatalogItemDetail(description, warrantyMonths, estimatedDurationMinutes, specifications);
        else
            Detail.Change(description, warrantyMonths, estimatedDurationMinutes, specifications);
        Touch(updatedBy);
    }

    public void AssignCategory(int categoryId, bool isPrimary = false, DateTime? addedAt = null)
    {
        if (isPrimary && _categories.Any(c => c.IsPrimary && c.CategoryId != categoryId))
            throw new DomainException("Item already has a primary category");

        var link = _categories.FirstOrDefault(c => c.CategoryId == categoryId);
        if (link is null)
            _categories.Add(new CatalogItemCategory(categoryId, isPrimary, addedAt));
        else
            link.MarkPrimary(isPrimary);
    }

    public void RemoveFromCategory(int categoryId)
    {
        var link = _categories.FirstOrDefault(c => c.CategoryId == categoryId)
            ?? throw new DomainException($"Item is not assigned to category '{categoryId}'");
        _categories.Remove(link);
    }

    public void MarkDeleted(string? updatedBy = null)
    {
        IsDeleted = true;
        IsActive = false;
        Touch(updatedBy);
    }

    private static decimal ValidPrice(decimal price)
    {
        if (price <= 0)
            throw new DomainException("Price must be positive");
        return decimal.Round(price, 2);
    }

    private int? ValidBrand(int? brandId)
    {
        if (brandId is not null && ItemType == CatalogItemType.Service)
            throw new DomainException("Service cannot have a brand");
        return brandId;
    }

    private void Touch(string? updatedBy)
    {
        if (Id == 0)
            return;

        UpdatedAt = Clock.UtcNow();
        UpdatedBy = Guard.Optional(updatedBy, AuthorMaxLength, "Author");
    }
}
