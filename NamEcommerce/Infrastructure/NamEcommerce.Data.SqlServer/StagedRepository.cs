namespace NamEcommerce.Data.SqlServer;

public sealed class StagedRepository<TEntity> : IRepository<TEntity> where TEntity : AppEntity
{
    private readonly NamEcommerceEfDbContext _context;

    public StagedRepository(NamEcommerceEfDbContext context)
    {
        _context = context;
    }

    public Task<TEntity> InsertAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<TEntity> UpdateAsync(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        var entry = _context.Entry(entity);
        if (entry.State == EntityState.Detached)
            _context.Update(entity);
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(TEntity entity)
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

    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        // FindAsync bỏ qua soft-delete query filter — tự lọc để giữ semantics như DataSource
        var entity = await _context.FindAsync<TEntity>(new object?[] { id }).ConfigureAwait(false);
        return entity is ISoftDeletable { IsDeleted: true } ? null : entity;
    }

    public Task<IEnumerable<TEntity>> GetAllAsync()
        => Task.FromResult<IEnumerable<TEntity>>(_context.Set<TEntity>().AsNoTracking());
}
