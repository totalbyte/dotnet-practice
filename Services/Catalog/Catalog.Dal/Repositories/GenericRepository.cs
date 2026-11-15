using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Catalog.Dal.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal.Repositories;

public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    protected readonly CatalogDbContext Context;
    protected readonly DbSet<TEntity> Set;

    public GenericRepository(CatalogDbContext context)
    {
        Context = context;
        Set = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default)
        => await Set.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
        => await Set.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<TEntity>> ListAsync(
        ISpecification<TEntity> specification, CancellationToken ct = default)
        => await Set.WithSpecification(specification).ToListAsync(ct);

    public Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification, CancellationToken ct = default)
        => Set.WithSpecification(specification).FirstOrDefaultAsync(ct);

    public Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
        => CriteriaOnly(specification).CountAsync(ct);

    public Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
        => CriteriaOnly(specification).AnyAsync(ct);

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public void Update(TEntity entity) => Set.Update(entity);

    public void Delete(TEntity entity) => Set.Remove(entity);

    private IQueryable<TEntity> CriteriaOnly(ISpecification<TEntity> specification)
        => SpecificationEvaluator.Default.GetQuery(Set, specification, evaluateCriteriaOnly: true);
}
