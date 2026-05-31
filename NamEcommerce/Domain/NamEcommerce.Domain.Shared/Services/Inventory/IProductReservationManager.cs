using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IProductReservationManager
{
    Task<decimal> GetTotalReservedAsync(Guid productId);

    Task<decimal> GetReservedForOrderAsync(Guid productId, Guid orderId);

    Task<decimal> GetReleasedByReferenceAsync(Guid productId, Guid orderId, ProductReservationReason reason, Guid referenceId);

    Task<decimal> GetReservedByReferenceAsync(Guid productId, Guid orderId, ProductReservationReason reason, Guid referenceId);

    Task<ProductReservationDto?> GetByProductIdAsync(Guid productId);

    Task ReserveAsync(Guid productId, decimal quantity, Guid orderId, ProductReservationReason reason, Guid? referenceId = null);

    Task ReleaseAsync(Guid productId, decimal quantity, Guid orderId, ProductReservationReason reason, Guid? referenceId = null);

    Task AdjustAsync(
        Guid productId,
        decimal deltaQuantity,
        Guid orderId,
        ProductReservationReason reserveReason,
        ProductReservationReason releaseReason,
        Guid? referenceId = null);
}
