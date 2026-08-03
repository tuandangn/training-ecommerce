using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Orders;

namespace NamEcommerce.Application.Contracts.Orders;

public interface IOrderFulfillmentScheduleAppService
{
    Task<OrderFulfillmentScheduleAppDto?> GetByIdAsync(Guid id);
    Task<IList<OrderFulfillmentScheduleAppDto>> GetByOrderIdAsync(Guid orderId, bool includeInactive = false);

    Task<CreateOrderFulfillmentScheduleResultAppDto> CreateAsync(CreateOrderFulfillmentScheduleAppDto dto);
    Task<UpdateOrderFulfillmentScheduleResultAppDto> UpdateAsync(UpdateOrderFulfillmentScheduleAppDto dto);
    Task<CommonActionResultDto> DeleteAsync(Guid id);

    Task<OrderFulfillmentBoardAppDto> GetBoardAsync(OrderFulfillmentBoardFilterAppDto filter);
    Task<decimal> GetActiveScheduledQuantityForOrderItemAsync(Guid orderId, Guid orderItemId);
    Task<CommonActionResultDto> CreateDefaultSchedulesForOrderAsync(Guid orderId, IList<Guid>? limitedOrderItemIds);
    Task DeleteScheduleItemsOfOrderItemsAsync(Guid orderId, IList<Guid> orderItemIds);
    Task<CommonActionResultDto> SetActiveAsync(SetOrderFulfillmentScheduleActiveAppDto dto);
    Task<CommonActionResultDto> RefreshWhenStockAvailableForPurchaseOrderItemsAsync(
        IReadOnlyCollection<(Guid purchaseOrderId, Guid purchaseOrderItemId)> purchaseOrderItemIds);
}
