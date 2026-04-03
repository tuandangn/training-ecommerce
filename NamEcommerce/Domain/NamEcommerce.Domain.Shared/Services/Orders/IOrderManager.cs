using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Shared.Services.Orders;

public interface IOrderManager
{
    Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto);
    Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto);
    Task AddOrderItemAsync(AddOrderItemDto dto);
    Task UpdateOrderItemAsync(UpdateOrderItemDto dto);
    Task DeleteOrderItemAsync(DeleteOrderItemDto dto);
    Task ChangeOrderStatusAsync(Guid orderId, int status);
    Task<OrderDto?> GetOrderByIdAsync(Guid id);
    Task<IPagedDataDto<OrderDto>> GetOrdersAsync(string? keywords, int pageIndex, int pageSize);
}
