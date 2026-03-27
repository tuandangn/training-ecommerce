namespace NamEcommerce.Domain.Shared.Common;

public interface IGetByIdService<TEntity> where TEntity : AppEntity
{
    Task<TEntity?> GetByIdAsync(Guid id);
}
