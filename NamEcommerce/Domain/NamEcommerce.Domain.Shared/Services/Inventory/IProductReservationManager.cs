using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IProductReservationManager
{
    Task<decimal> GetTotalReservedAsync(Guid productId);

    Task<ProductReservationDto?> GetByProductIdAsync(Guid productId);

    Task ReserveAsync(Guid productId, decimal quantity, Guid orderId);

    Task ReleaseAsync(Guid productId, decimal quantity, Guid orderId);

    Task AdjustAsync(Guid productId, decimal deltaQuantity, Guid orderId);
}
