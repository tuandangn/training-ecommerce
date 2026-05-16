using Microsoft.EntityFrameworkCore.Storage;

namespace NamEcommerce.Data.SqlServer;

public sealed class NamEcommerceEfDbContext : DbContext, IDbContext
{
    public NamEcommerceEfDbContext(DbContextOptions<NamEcommerceEfDbContext> opts) : base(opts)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NamEcommerceEfDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(GetSoftDeleteFilter(entityType.ClrType));
            }
        }

        base.OnModelCreating(modelBuilder);
    }
    private static LambdaExpression GetSoftDeleteFilter(Type type)
    {
        var parameter = Expression.Parameter(type, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
        var falseConstant = Expression.Constant(false);
        var equal = Expression.Equal(property, falseConstant);
        return Expression.Lambda(equal, parameter);
    }

    public IQueryable<TEntity> GetDataSource<TEntity>(bool includeHidden) where TEntity : AppAggregateEntity
        => includeHidden ? Set<TEntity>().IgnoreQueryFilters().AsNoTracking() : Set<TEntity>().AsNoTracking();

    public async Task<TEntity?> FindAsync<TEntity>(Guid key, CancellationToken cancellationToken = default)
        where TEntity : AppAggregateEntity
    {
        var entity = await FindAsync<TEntity>([key]).ConfigureAwait(false);
        return entity;
    }

    public Task<IEnumerable<TEntity>> GetDataAsync<TEntity>() where TEntity : AppAggregateEntity
    {
        IQueryable<TEntity> entities = Set<TEntity>().AsNoTracking();
        return Task.FromResult<IEnumerable<TEntity>>(entities);
    }

    async Task<TEntity> IDbContext.AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
    {
        await AddAsync((object)entity, cancellationToken).ConfigureAwait(false);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    async Task IDbContext.RemoveAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.Delete();
            Update(entity);
        }
        else
        {
            Remove(entity);
        }
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    async Task<TEntity> IDbContext.UpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken)
    {
        Update(entity);
        await SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity;
    }

    async Task<IDataTransaction> IDbContext.BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        return new EfDataTransaction(transaction);
    }

    private sealed class EfDataTransaction(IDbContextTransaction transaction) : IDataTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
            => transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync()
            => transaction.DisposeAsync();
    }
}
