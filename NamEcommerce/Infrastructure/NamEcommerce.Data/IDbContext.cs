using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Data.Contracts;

public interface IDbContext
{
    IQueryable<TEntity> GetData<TEntity>() where TEntity : AppAggregateEntity;

    ValueTask<TEntity?> FindAsync<TEntity, TKey>(TKey[] keyValues, CancellationToken cancellationToken = default)
        where TEntity : AppAggregateEntity;
    ValueTask<TEntity?> FindAsync<TEntity>(int key, CancellationToken cancellationToken = default)
        where TEntity : AppAggregateEntity;

    ValueTask<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : AppAggregateEntity;

    void Remove<TEntity>(TEntity entity) where TEntity : AppAggregateEntity;

    TEntity Update<TEntity>(TEntity entity)
             where TEntity : AppAggregateEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
