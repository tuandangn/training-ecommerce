namespace NamEcommerce.Domain.Shared.Services;

public interface IEntityDataReader<TEntity> where TEntity : AppAggregateEntity
{
    IQueryable<TEntity> DataSource { get; }

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
