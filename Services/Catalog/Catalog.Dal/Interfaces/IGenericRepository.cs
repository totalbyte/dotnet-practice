using Ardalis.Specification;

namespace Catalog.Dal.Interfaces;

public interface IGenericRepository<TEntity, in TKey>
    where TEntity : class
    where TKey : notnull
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
    Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    void Update(TEntity entity);
    void Delete(TEntity entity);
}
