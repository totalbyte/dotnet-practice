using Catalog.Domain.Entities;

namespace Catalog.Dal.Interfaces;

public interface ICategoryRepository : IGenericRepository<Category, int>
{
    Task<Category?> GetWithItemsAsync(int id, CancellationToken ct = default);
}
