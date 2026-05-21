using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Shared.Common;

public interface IEntityDataReader<TEntity> : IGetByIdService<TEntity> where TEntity : AppAggregateEntity
{
    IQueryable<TEntity> DataSource { get; }
    IQueryable<TEntity> SecuredDataSource { get; }

    Task<IEnumerable<TEntity>> GetAllAsync();

    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids);

    IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec);

    Task<IList<TEntity>> GetListAsync(ISpecification<TEntity> spec);

    Task<bool> AnyAsync(ISpecification<TEntity> spec);
}
