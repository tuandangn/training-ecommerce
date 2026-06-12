using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Data.SqlServer;

public sealed class EntityDataReader<TEntity> : IEntityDataReader<TEntity> where TEntity : AppEntity
{
    private readonly IDbContext _dbContext;

    public EntityDataReader(IDbContext db) => _dbContext = db;

    public IQueryable<TEntity> DataSource => _dbContext.GetDataSource<TEntity>().AsNoTracking();

    public IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec)
    {
        var query = DataSource.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query,
            (current, include) => current.Include(include));

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending is not null)
            query = query.OrderByDescending(spec.OrderByDescending);

        return query;
    }

    public async Task<IList<TEntity>> GetListAsync(ISpecification<TEntity> spec)
        => await ApplySpecification(spec).ToListAsync().ConfigureAwait(false);

    public async Task<IList<TEntity>> GetPagedListAsync(ISpecification<TEntity> spec, int pageIndex, int pageSize)
    {
        return await ApplySpecification(spec)
                .Skip(pageIndex * pageSize).Take(pageSize)
                .ToListAsync().ConfigureAwait(false);
    }

    public async Task<int> CountAsync(ISpecification<TEntity> spec)
        => await ApplySpecification(spec).CountAsync().ConfigureAwait(false);

    public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> spec)
        => ApplySpecification(spec).FirstOrDefaultAsync();

    public async Task<bool> AnyAsync(ISpecification<TEntity> spec)
        => await DataSource.AnyAsync(spec.Criteria).ConfigureAwait(false);

    public IQueryable<TEntity> SecuredDataSource => ((NamEcommerceEfDbContext)_dbContext).Set<TEntity>().IgnoreQueryFilters().AsNoTracking();

    public Task<IEnumerable<TEntity>> GetAllAsync()
        => _dbContext.GetDataAsync<TEntity>();

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => DataSource.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        if (!ids.Any())
            return [];

        var query = from entity in DataSource
                    where ids.Contains(entity.Id)
                    select entity;

        return await query.ToListAsync().ConfigureAwait(false);
    }
}
