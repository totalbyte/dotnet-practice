using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface IUnitOfWork
{
    ICatalogItemRepository CatalogItems { get; }
    ICategoryRepository Categories { get; }
    ICatalogItemCategoryRepository CatalogItemCategories { get; }
    IGenericRepository<Brand, int> Brands { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
