using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Orders;

public interface IOrderAppService
{
    Task<OrderAppDto?> GetOrderByIdAsync(Guid id);
    Task<IPagedDataAppDto<OrderAppDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords, int? status);

    Task<CreateOrderResultAppDto> CreateOrderAsync(CreateOrderAppDto dto);
    Task<UpdateOrderResultAppDto> UpdateOrderAsync(UpdateOrderAppDto dto);
    Task<DeleteOrderResultAppDto> DeleteOrderAsync(DeleteOrderAppDto dto);

    Task<LockOrderResultAppDto> LockOrderAsync(LockOrderAppDto dto);
    Task<UpdateOrderShippingResultAppDto> UpdateShippingAsync(UpdateOrderShippingAppDto dto);

    Task<AddOrderItemResultAppDto> AddOrderItemAsync(AddOrderItemAppDto dto);
    Task<UpdateOrderItemResultAppDto> UpdateOrderItemAsync(UpdateOrderItemAppDto dto);
    Task<DeleteOrderItemResultAppDto> DeleteOrderItemAsync(DeleteOrderItemAppDto dto);

    Task<MarkOrderItemDeliveredResultAppDto> MarkOrderItemDeliveredAsync(MarkOrderItemDeliveredAppDto dto);

    Task<string> NextOrderCodeAsync();
}
