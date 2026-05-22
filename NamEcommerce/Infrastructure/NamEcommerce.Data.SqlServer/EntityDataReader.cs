using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Data.SqlServer;

public sealed class EntityDataReader<TEntity> : IEntityDataReader<TEntity> where TEntity : AppAggregateEntity
{
    private readonly NamEcommerceEfDbContext _dbContext;

    public EntityDataReader(NamEcommerceEfDbContext db) => _dbContext = db;

    public IQueryable<TEntity> DataSource => _dbContext.Set<TEntity>().AsNoTracking();

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
        => await ApplySpecification(spec).ToListAsync();

    public async Task<bool> AnyAsync(ISpecification<TEntity> spec)
        => await DataSource.AnyAsync(spec.Criteria);

    public IQueryable<TEntity> SecuredDataSource => _dbContext.Set<TEntity>().IgnoreQueryFilters().AsNoTracking();

    public Task<IEnumerable<TEntity>> GetAllAsync()
        => _dbContext.GetDataAsync<TEntity>();

    public Task<TEntity?> GetByIdAsync(Guid id)
        => _dbContext.FindAsync<TEntity>(id);

    public Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        if (!ids.Any())
            return Task.FromResult(Enumerable.Empty<TEntity>());

        var query = from entity in DataSource
                    where ids.Contains(entity.Id)
                    select entity;

        return Task.FromResult<IEnumerable<TEntity>>(query);
    }
}
