using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Orders;

public interface IOrderAppService
{
    Task<CreateOrderResultAppDto> CreateOrderAsync(CreateOrderAppDto dto);
    Task<UpdateOrderResultAppDto> UpdateOrderAsync(UpdateOrderAppDto dto);
    Task<AddOrderItemResultAppDto> AddOrderItemAsync(AddOrderItemAppDto dto);
    Task<UpdateOrderItemResultAppDto> UpdateOrderItemAsync(UpdateOrderItemAppDto dto);
    Task<DeleteOrderItemResultAppDto> DeleteOrderItemAsync(DeleteOrderItemAppDto dto);
    Task ChangeOrderStatusAsync(Guid orderId, int status);
    Task<OrderAppDto?> GetOrderByIdAsync(Guid id);
    Task<IPagedDataAppDto<OrderAppDto>> GetOrdersAsync(string? keywords, int pageIndex, int pageSize);
}
