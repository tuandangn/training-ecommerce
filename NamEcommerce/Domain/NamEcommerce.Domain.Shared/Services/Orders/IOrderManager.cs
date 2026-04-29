using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Services.Orders;

public interface IOrderManager : ICodeExistCheckingService
{
    Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto);
    Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto);
    Task DeleteOrderAsync(DeleteOrderDto dto);

    Task AddOrderItemAsync(Guid orderId, AddOrderItemDto dto);
    Task UpdateOrderItemAsync(UpdateOrderItemDto dto);
    Task DeleteOrderItemAsync(DeleteOrderItemDto dto);

    Task UpdateShippingAsync(UpdateShippingDto dto);
    Task LockOrderAsync(LockOrderDto dto);
    Task MarkOrderItemDeliveredAsync(MarkOrderItemDeliveredDto dto);

    Task<OrderDto?> GetOrderByIdAsync(Guid id);
    Task<IPagedDataDto<OrderDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords, OrderStatus? status);
    Task<IPagedDataDto<OrderDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords, IEnumerable<OrderStatus> status);
}
