using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Data.Contracts;

public interface IRepository<TEntity> : IGetByIdService<TEntity> where TEntity : AppEntity
{
    Task<IEnumerable<TEntity>> GetAllAsync();

    Task<TEntity?> GetByIdAsync(Guid id);

    Task<TEntity> InsertAsync(TEntity entity);

    Task<TEntity> UpdateAsync(TEntity entity);

    Task DeleteAsync(TEntity entity);
}
