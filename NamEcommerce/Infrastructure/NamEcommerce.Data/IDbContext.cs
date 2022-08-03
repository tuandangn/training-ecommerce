using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Data.Contracts;

public interface IDbContext
{
    Task<IEnumerable<TEntity>> GetDataAsync<TEntity>() where TEntity : AppAggregateEntity;

    Task<TEntity?> FindAsync<TEntity>(Guid key, CancellationToken cancellationToken = default)
        where TEntity : AppAggregateEntity;

    Task<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : AppAggregateEntity;

    Task RemoveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : AppAggregateEntity;

    Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
             where TEntity : AppAggregateEntity;
}
