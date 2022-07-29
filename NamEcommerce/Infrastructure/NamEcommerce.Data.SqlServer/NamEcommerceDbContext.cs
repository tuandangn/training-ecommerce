namespace NamEcommerce.Data.SqlServer;

public sealed class NamEcommerceDbContext : DbContext, IDbContext
{
    public NamEcommerceDbContext(DbContextOptions<NamEcommerceDbContext> opts) : base(opts)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NamEcommerceDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public IQueryable<TEntity> GetData<TEntity>() where TEntity : AppAggregateEntity
        => Set<TEntity>();

    async ValueTask<TEntity> IDbContext.AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
    {
        var entry = await AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return entry.Entity;
    }

    void IDbContext.Remove<TEntity>(TEntity entity)
        => Remove(entity);

    TEntity IDbContext.Update<TEntity>(TEntity entity)
    {
        var entry = Update(entity);
        return entry.Entity;
    }

    public ValueTask<TEntity?> FindAsync<TEntity, TKey>(TKey[] keyValues, CancellationToken cancellationToken = default) 
        where TEntity : AppAggregateEntity
    {
        object[] keys = new object[keyValues.Length];
        Array.Copy(keyValues, keys, keyValues.Length);
        return FindAsync<TEntity>(keys, cancellationToken);
    }

    public ValueTask<TEntity?> FindAsync<TEntity>(int key, CancellationToken cancellationToken = default) 
        where TEntity : AppAggregateEntity
        => FindAsync<TEntity, int>(new[] { key }, cancellationToken);
}
