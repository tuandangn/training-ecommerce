namespace NamEcommerce.Data.SqlServer;

public sealed class StagedRepository<TEntity> : IRepository<TEntity> where TEntity : AppEntity
{
    private readonly NamEcommerceEfDbContext _context;

    public StagedRepository(NamEcommerceEfDbContext context)
    {
        _context = context;
    }

    public Task<TEntity> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
            _context.Update(entity);
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.Delete();
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
                _context.Update(entity);
        }
        else
        {
            _context.Remove(entity);
        }
        return Task.CompletedTask;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.FindAsync<TEntity>(new object?[] { id }, cancellationToken).ConfigureAwait(false);

    public Task<IEnumerable<TEntity>> GetAllAsync()
        => Task.FromResult<IEnumerable<TEntity>>(_context.Set<TEntity>().AsNoTracking());
}
