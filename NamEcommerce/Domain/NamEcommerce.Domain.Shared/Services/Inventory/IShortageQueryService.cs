using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

public interface IShortageQueryService
{
    Task<IList<OrderItemShortageDto>> GetOrderItemShortagesAsync(Guid orderId);
    Task<IList<OrderItemFulfillmentStateDto>> GetOrderItemFulfillmentStatesAsync(Guid orderId);
    Task<IList<DeliveryNoteItemShortageDto>> GetDeliveryNoteShortagesAsync(Guid deliveryNoteId);
    Task<IList<OrderItemShortageDto>> GetGlobalShortagesAsync(ShortageFilterDto? filter);
}
