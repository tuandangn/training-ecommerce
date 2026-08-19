using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Data.SqlServer;

public sealed class EntityDataReader<TEntity> : IEntityDataReader<TEntity> where TEntity : AppEntity
{
    private readonly IDbContext _dbContext;

    public EntityDataReader(IDbContext db) => _dbContext = db;

    public IQueryable<TEntity> GetDataSource(ReaderDataSourceOptions options)
    {
        if (_dbContext is not NamEcommerceEfDbContext efDbContext)
            return DataSource;

        IQueryable<TEntity> baseDataSource = efDbContext.Set<TEntity>();
        if (!options.IncludeDeleted)
            baseDataSource = baseDataSource.IgnoreQueryFilters();
        if (!options.ReadWrite)
            baseDataSource = baseDataSource.AsNoTracking();

        return baseDataSource;
    }

    public IQueryable<TEntity> DataSource => _dbContext.GetDataSource<TEntity>().AsNoTracking();

    public IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default)
    {
        var query = GetDataSource(opts).Where(spec.Criteria);

        query = spec.Includes.Aggregate(query,
            (current, include) => current.Include(include));

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        return query;
    }

    public async Task<IList<TEntity>> GetListAsync(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default)
        => await ApplySpecification(spec, opts).ToListAsync().ConfigureAwait(false);

    public async Task<IList<TEntity>> GetPagedListAsync(ISpecification<TEntity> spec, int pageIndex, int pageSize, ReaderDataSourceOptions opts = default)
    {
        return await ApplySpecification(spec, opts)
                .Skip(pageIndex * pageSize).Take(pageSize)
                .ToListAsync().ConfigureAwait(false);
    }

    public async Task<int> CountAsync(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default)
        => await ApplySpecification(spec, opts).CountAsync().ConfigureAwait(false);

    public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default)
        => ApplySpecification(spec, opts).FirstOrDefaultAsync();

    public async Task<bool> AnyAsync(ISpecification<TEntity> spec, ReaderDataSourceOptions opts = default)
        => await GetDataSource(opts).AnyAsync(spec.Criteria).ConfigureAwait(false);

    public async Task<IEnumerable<TEntity>> GetAllAsync(ReaderDataSourceOptions opts = default)
        => await GetDataSource(opts).ToListAsync().ConfigureAwait(false);

    public Task<TEntity?> GetByIdAsync(Guid id)
        => GetDataSource(new() { ReadWrite = true }).FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids, ReaderDataSourceOptions? opts = null)
    {
        if (!ids.Any())
            return [];

        return await GetDataSource(opts ?? new() { ReadWrite = true })
                    .Where(entity => ids.Contains(entity.Id))
                    .ToListAsync().ConfigureAwait(false);
    }
}
