namespace NamEcommerce.Data.Contracts;

public interface IDbContext
{
    IQueryable<TEntity> GetData<TEntity>() where TEntity : class;

    ValueTask<TEntity?> FindAsync<TEntity, TKey>(TKey[] keyValues, CancellationToken cancellationToken = default)
        where TEntity : class;
    ValueTask<TEntity?> FindAsync<TEntity>(int key, CancellationToken cancellationToken = default)
        where TEntity : class;

    ValueTask<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : class;

    void Remove<TEntity>(TEntity entity) where TEntity : class;

    TEntity Update<TEntity>(TEntity entity)
             where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
