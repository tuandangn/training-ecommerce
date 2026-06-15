using NamEcommerce.Domain.Shared.Dtos.Orders;

namespace NamEcommerce.Domain.Shared.Services.Orders;

public interface IOrderFulfillmentScheduleManager
{
    Task<OrderFulfillmentScheduleDto?> GetByIdAsync(Guid id);
    Task<IList<OrderFulfillmentScheduleDto>> GetByOrderIdAsync(Guid orderId);
    Task<IList<OrderFulfillmentScheduleDto>> GetActiveByOrderIdsAsync(IReadOnlyCollection<Guid> orderIds);
    Task<CreateOrderFulfillmentScheduleResultDto> CreateAsync(CreateOrderFulfillmentScheduleDto dto);
    Task<UpdateOrderFulfillmentScheduleResultDto> UpdateAsync(UpdateOrderFulfillmentScheduleDto dto);
    Task SetActiveAsync(SetOrderFulfillmentScheduleActiveDto dto);
    Task DeleteAsync(Guid id);
    Task RefreshWhenStockAvailableAsync(IReadOnlyCollection<Guid> orderItemIds);
}
