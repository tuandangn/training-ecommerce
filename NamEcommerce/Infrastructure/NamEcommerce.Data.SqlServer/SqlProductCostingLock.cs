using Microsoft.EntityFrameworkCore;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Data.SqlServer;

public sealed class SqlProductCostingLock(NamEcommerceEfDbContext dbContext) : IProductCostingLock
{
    public async Task<IAsyncDisposable> AcquireAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var resource = $"inv-cost-{productId:N}";
        var result = await dbContext.Database.ExecuteSqlRawAsync(
            "EXEC sp_getapplock @Resource = {0}, @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = 5000",
            resource).ConfigureAwait(false);

        if (result < 0)
            throw new InvalidOperationException($"Failed to acquire costing lock for product {productId}. sp_getapplock returned {result}.");

        return new LockHandle(dbContext, resource);
    }

    private sealed class LockHandle(NamEcommerceEfDbContext dbContext, string resource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_releaseapplock @Resource = {0}, @LockOwner = N'Session'",
                resource).ConfigureAwait(false);
        }
    }
}
