using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Data.Contracts;

public interface IDbContext
{
    IQueryable<TEntity> GetDataSource<TEntity>(bool includeHidden = false) where TEntity : AppEntity;

    Task<IEnumerable<TEntity>> GetDataAsync<TEntity>() where TEntity : AppEntity;

    Task<TEntity?> FindAsync<TEntity>(Guid key, CancellationToken cancellationToken = default)
        where TEntity : AppEntity;

    Task<TEntity> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        where TEntity : AppEntity;

    Task RemoveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : AppEntity;

    Task<TEntity> UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
             where TEntity : AppEntity;

    Task<IDataTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
