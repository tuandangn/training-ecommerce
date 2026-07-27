namespace NamEcommerce.Data.SqlServer;

public sealed class NamEcommerceEfRepository<TEntity> : IRepository<TEntity> where TEntity : AppEntity
{
    private readonly IDbContext _dbContext;

    public NamEcommerceEfRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task DeleteAsync(TEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        return _dbContext.RemoveAsync(entity);
    }

    public Task<IEnumerable<TEntity>> GetAllAsync()
        => _dbContext.GetDataAsync<TEntity>();

    public Task<TEntity?> GetByIdAsync(Guid id)
        => _dbContext.FindAsync<TEntity>(id);

    public Task<TEntity> InsertAsync(TEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        return _dbContext.AddAsync(entity);
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        return _dbContext.UpdateAsync(entity);
    }
}
