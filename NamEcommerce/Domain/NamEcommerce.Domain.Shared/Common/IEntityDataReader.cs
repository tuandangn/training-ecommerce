using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Shared.Common;

public interface IEntityDataReader<TEntity> : IGetByIdService<TEntity> where TEntity : AppEntity
{
    IQueryable<TEntity> DataSource { get; }
    IQueryable<TEntity> TrackingDataSource { get; }

    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids);

    IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec);

    Task<bool> AnyAsync(ISpecification<TEntity> spec);
    Task<int> CountAsync(ISpecification<TEntity> spec);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec);
    Task<IList<TEntity>> GetListAsync(ISpecification<TEntity> spec);
    Task<IList<TEntity>> GetPagedListAsync(ISpecification<TEntity> spec, int pageIndex, int pageSize);
}
