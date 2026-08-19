using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Shared.Common;

public interface IEntityDataReader<TEntity> : IGetByIdService<TEntity> where TEntity : AppEntity
{
    IQueryable<TEntity> DataSource { get; }
    IQueryable<TEntity> GetDataSource(ReaderDataSourceOptions options);

    Task<IEnumerable<TEntity>> GetAllAsync(ReaderDataSourceOptions opts = default);
    Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids, ReaderDataSourceOptions? opts = null);

    IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default);

    Task<bool> AnyAsync(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default);
    Task<int> CountAsync(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default);
    Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default);
    Task<IList<TEntity>> GetListAsync(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default);
    Task<IList<TEntity>> GetPagedListAsync(ISpecification<TEntity> spec, int pageIndex, int pageSize, ReaderDataSourceOptions opts = default);
}

[Serializable]
public readonly record struct ReaderDataSourceOptions
{
    public bool ReadWrite { get; init; }
    public bool IncludeDeleted { get; init; }
}
