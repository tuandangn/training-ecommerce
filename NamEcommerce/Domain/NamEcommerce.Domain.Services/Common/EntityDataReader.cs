using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Common;

public sealed class EntityDataReader<TEntity> : IEntityDataReader<TEntity> where TEntity : AppAggregateEntity
{
    private readonly IDbContext _dbContext;
    private readonly IRepository<TEntity> _repository;

    public EntityDataReader(IDbContext dbContext, IRepository<TEntity> repository)
    {
        _dbContext = dbContext;
        _repository = repository;
    }

    public IQueryable<TEntity> DataSource => _dbContext.GetDataSource<TEntity>();

    public Task<IEnumerable<TEntity>> GetAllAsync()
        => _repository.GetAllAsync();

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<IEnumerable<TEntity>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        if(ids.Count() == 0)
            return Task.FromResult(Enumerable.Empty<TEntity>());

        var query = from entity in DataSource
                    where ids.Contains(entity.Id)
                    select entity;

        return Task.FromResult<IEnumerable<TEntity>>(query);
    }
}
