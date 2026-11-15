using Catalog.Dal.Interfaces;
using Catalog.Dal.Repositories;
using Catalog.Domain.Entities;
using Catalog.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal;

public class UnitOfWork : IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;
    private const int ConstraintViolation = 547;

    private readonly CatalogDbContext _context;

    public UnitOfWork(CatalogDbContext context)
    {
        _context = context;
        CatalogItems = new CatalogItemRepository(context);
        Categories = new CategoryRepository(context);
        CatalogItemCategories = new CatalogItemCategoryRepository(context);
        Brands = new GenericRepository<Brand, int>(context);
    }

    public ICatalogItemRepository CatalogItems { get; }
    public ICategoryRepository Categories { get; }
    public ICatalogItemCategoryRepository CatalogItemCategories { get; }
    public IGenericRepository<Brand, int> Brands { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictException("The record was changed or deleted by another request", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql)
        {
            throw sql.Number switch
            {
                UniqueIndexViolation or UniqueConstraintViolation =>
                    new ConflictException("A record with the same unique value already exists", ex),
                ConstraintViolation =>
                    new ConflictException("The change violates a database constraint", ex),
                _ => ex
            };
        }
    }
}
