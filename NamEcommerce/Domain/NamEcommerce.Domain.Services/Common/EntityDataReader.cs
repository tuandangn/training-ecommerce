using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Common;

public sealed class EntityDataReader<TEntity> : IEntityDataReader<TEntity> where TEntity : AppAggregateEntity
{
    private readonly IDbContext _dbContext;

    public EntityDataReader(IDbContext dbContext) 
        => _dbContext = dbContext;

    public IQueryable<TEntity> DataSource => _dbContext.GetDataSource<TEntity>();

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.FindAsync<TEntity>(id, cancellationToken);
}
