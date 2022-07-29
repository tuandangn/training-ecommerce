namespace NamEcommerce.Data.SqlServer;

public sealed class EfRepository<TEntity> : IRepository<TEntity> where TEntity : AppAggregateEntity
{
    private readonly IDbContext _dbContext;

    public EfRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task DeleteAsync(TEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        _dbContext.Remove(entity);
        return _dbContext.SaveChangesAsync();
    }

    public Task<IEnumerable<TEntity>> GetAllAsync()
    {
        IEnumerable<TEntity> data = _dbContext.GetData<TEntity>();
        return Task.FromResult(data);
    }

    public Task<TEntity?> GetByIdAsync(int id)
        => _dbContext.FindAsync<TEntity>(id).AsTask();

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var addingEntity = await _dbContext.AddAsync(entity).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        return addingEntity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var updatingEntity = _dbContext.Update(entity);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return updatingEntity;
    }
}
