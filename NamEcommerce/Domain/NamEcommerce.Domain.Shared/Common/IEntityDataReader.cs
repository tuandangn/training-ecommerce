namespace NamEcommerce.Domain.Shared.Common;

public interface IEntityDataReader<TEntity> : IGetByIdService<TEntity> where TEntity : AppAggregateEntity
{
    IQueryable<TEntity> DataSource { get; }

    Task<IEnumerable<TEntity>> GetAllAsync();

    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids);
}
