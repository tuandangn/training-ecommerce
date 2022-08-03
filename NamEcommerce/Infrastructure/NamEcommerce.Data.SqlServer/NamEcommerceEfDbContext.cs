namespace NamEcommerce.Data.SqlServer;

public sealed class NamEcommerceEfDbContext : DbContext, IDbContext
{
    public NamEcommerceEfDbContext(DbContextOptions<NamEcommerceEfDbContext> opts) : base(opts)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NamEcommerceEfDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public async Task<TEntity?> FindAsync<TEntity>(Guid key, CancellationToken cancellationToken = default) 
        where TEntity : AppAggregateEntity
    {
        var entity = await FindAsync<TEntity>(new[] { key }).ConfigureAwait(false);
        return entity;
    }

    public Task<IEnumerable<TEntity>> GetDataAsync<TEntity>() where TEntity : AppAggregateEntity
    {
        IEnumerable<TEntity> entities = Set<TEntity>();
        return Task.FromResult(entities);
    }

    async Task<TEntity> IDbContext.AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
    {
        await AddAsync((object)entity, cancellationToken).ConfigureAwait(false);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    async Task IDbContext.RemoveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
    {
        Remove(entity);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task<TEntity> IDbContext.UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
    {
        Update(entity);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }
}
