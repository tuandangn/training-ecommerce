using Microsoft.EntityFrameworkCore;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Data.SqlServer;

public sealed class SqlProductCostingLock(NamEcommerceEfDbContext dbContext) : IProductCostingLock
{
    public async Task<IAsyncDisposable> AcquireAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var resource = $"inv-cost-{productId:N}";

        // Applock owner 'Session' gắn với connection — phải giữ connection mở suốt vòng đời lock,
        // nếu không EF đóng connection sau mỗi lệnh và lock bị mất (hoặc kẹt trên pooled connection).
        await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // ExecuteSqlRawAsync trả về rows affected (luôn -1 với EXEC), không phải return value
            // của sp_getapplock — phải kiểm tra kết quả trong SQL và THROW khi không lấy được lock.
            await dbContext.Database.ExecuteSqlRawAsync(
                """
                DECLARE @result int;
                EXEC @result = sp_getapplock @Resource = {0}, @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = 5000;
                IF @result < 0
                    THROW 51000, N'sp_getapplock did not grant the lock.', 1;
                """,
                new object[] { resource },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Failed to acquire costing lock for product {productId}.", ex);
        }

        return new LockHandle(dbContext, resource);
    }

    private sealed class LockHandle(NamEcommerceEfDbContext dbContext, string resource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_releaseapplock @Resource = {0}, @LockOwner = N'Session'",
                    resource).ConfigureAwait(false);
            }
            finally
            {
                await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
    }
}
