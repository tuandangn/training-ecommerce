using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Services.Orders;

public interface IOrderManager : ICodeExistCheckingService
{
    Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto);
    Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto);

    Task AddOrderItemAsync(Guid orderId, AddOrderItemDto dto);
    Task UpdateOrderItemAsync(UpdateOrderItemDto dto);
    Task DeleteOrderItemAsync(DeleteOrderItemDto dto);
    Task ChangeOrderStatusAsync(Guid orderId, OrderStatus status);
    
    Task MarkAsPaidAsync(MarkAsPaidDto dto);
    Task UpdateShippingAsync(UpdateShippingDto dto);
    Task CancelOrderAsync(CancelOrderDto dto);

    Task<OrderDto?> GetOrderByIdAsync(Guid id);
    Task<IPagedDataDto<OrderDto>> GetOrdersAsync(string? keywords, OrderStatus? status, int pageIndex, int pageSize);

    Task VerifyStatusAsync(Guid id);
}
