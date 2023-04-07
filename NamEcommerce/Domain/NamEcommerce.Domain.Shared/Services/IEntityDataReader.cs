namespace NamEcommerce.Domain.Shared.Services;

public interface IEntityDataReader<TEntity> where TEntity : AppAggregateEntity
{
    IQueryable<TEntity> DataSource { get; }

    Task<IEnumerable<TEntity>> GetAllAsync();

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids);
}
