namespace NamEcommerce.Data.SqlServer;

public sealed class EfRepository<TEntity> : IRepository<TEntity> where TEntity : AppAggregateEntity
{
    private readonly IDbContext _dbContext;

    public EfRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        return _dbContext.RemoveAsync(entity, cancellationToken);
    }

    public Task<IEnumerable<TEntity>> GetAllAsync() 
        => _dbContext.GetDataAsync<TEntity>();

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => _dbContext.FindAsync<TEntity>(id, cancellationToken);

    public Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        return _dbContext.AddAsync(entity, cancellationToken);
    }

    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        return _dbContext.UpdateAsync(entity, cancellationToken);
    }
}
