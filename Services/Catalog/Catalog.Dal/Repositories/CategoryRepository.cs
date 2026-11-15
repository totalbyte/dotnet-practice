using Catalog.Dal.Interfaces;
using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class CategoryRepository(CatalogDbContext context)
    : GenericRepository<Category, int>(context), ICategoryRepository
{
    public async Task<Category?> GetWithItemsAsync(int id, CancellationToken ct = default)
    {
        var category = await Set.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
            return null;

        await Context.Entry(category)
            .Collection(c => c.ItemCategories)
            .Query()
            .Include(ic => ic.CatalogItem)
            .LoadAsync(ct);

        return category;
    }
}
