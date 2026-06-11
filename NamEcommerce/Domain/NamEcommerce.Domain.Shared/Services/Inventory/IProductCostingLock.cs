namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IProductCostingLock
{
    Task<IAsyncDisposable> AcquireAsync(Guid productId, CancellationToken cancellationToken = default);
}
