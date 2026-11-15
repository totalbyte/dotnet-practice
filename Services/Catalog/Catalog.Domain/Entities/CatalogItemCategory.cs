namespace Catalog.Domain.Entities;

public class CatalogItemCategory
{
    private CatalogItemCategory()
    {
    }

    internal CatalogItemCategory(int categoryId, bool isPrimary, DateTime? addedAt)
    {
        CategoryId = categoryId;
        IsPrimary = isPrimary;
        AddedAt = addedAt ?? Clock.UtcNow();
    }

    public long CatalogItemId { get; private set; }
    public CatalogItem CatalogItem { get; private set; } = null!;
    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public DateTime AddedAt { get; private set; }
    public bool IsPrimary { get; private set; }

    internal void MarkPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
