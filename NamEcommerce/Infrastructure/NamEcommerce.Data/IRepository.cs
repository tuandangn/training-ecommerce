using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Data.Contracts;

public interface IRepository<TEntity> : IGetByIdService<TEntity> where TEntity : AppEntity
{
    Task<IEnumerable<TEntity>> GetAllAsync();

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
