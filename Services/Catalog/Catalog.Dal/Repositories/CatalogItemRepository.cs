using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class CatalogItemRepository(CatalogDbContext context)
    : GenericRepository<CatalogItem, long>(context), ICatalogItemRepository
{
    public Task<CatalogItem?> GetWithDetailsAsync(long id, CancellationToken ct = default)
        => Set.Include(i => i.Brand)
              .Include(i => i.Detail)
              .Include(i => i.Categories).ThenInclude(ic => ic.Category)
              .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<bool> ExistsBySkuAsync(string sku, CancellationToken ct = default)
        => Set.IgnoreQueryFilters().AnyAsync(i => i.Sku == sku, ct);
}
