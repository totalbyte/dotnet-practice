using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class CatalogItemCategoryRepository(CatalogDbContext context)
    : GenericRepository<CatalogItemCategory, (long CatalogItemId, int CategoryId)>(context),
      ICatalogItemCategoryRepository
{
    public override async Task<CatalogItemCategory?> GetByIdAsync(
        (long CatalogItemId, int CategoryId) id, CancellationToken ct = default)
        => await Set.FindAsync(new object[] { id.CatalogItemId, id.CategoryId }, ct);

    public async Task<IReadOnlyList<CatalogItem>> GetRecentlyAddedAsync(
        string categorySlug, DateTime since, CancellationToken ct = default)
        => await Set
            .Where(ic => ic.Category.Slug == categorySlug && ic.AddedAt >= since)
            .OrderByDescending(ic => ic.AddedAt)
            .Select(ic => ic.CatalogItem)
            .AsNoTracking()
            .ToListAsync(ct);
}
