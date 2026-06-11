using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Services.Orders;

public interface IOrderManager : ICodeExistCheckingService
{
    Task<OrderDto?> GetOrderByIdAsync(Guid id);
    Task<IPagedDataDto<OrderDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords, OrderStatus? status);
    Task<IPagedDataDto<OrderDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords, IEnumerable<OrderStatus> status);

    Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto);
    Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto);
    Task MarkOrderItemDeliveredAsync(MarkOrderItemDeliveredDto dto);
    Task MarkOrderItemReceivedByCustomerAsync(MarkOrderItemReceivedByCustomerDto dto);
    Task CompleteOrderAsync(CompleteOrderDto dto);
    Task RequestQuickSaleDeliveryAsync(Guid orderId, Guid deliveryNoteId, DateTime requestedAtUtc);
    Task DeleteOrderAsync(DeleteOrderDto dto);
    Task CancelOrderAsync(CancelOrderDto dto);

    Task AddOrderItemAsync(Guid orderId, AddOrderItemDto dto);
    Task UpdateOrderItemAsync(UpdateOrderItemDto dto);
    Task DeleteOrderItemAsync(DeleteOrderItemDto dto);

    Task UpdateShippingAsync(UpdateShippingDto dto);

    //**TODO** move to price module
    Task<IList<RecentSalePriceDto>> GetRecentSalePricesAsync(Guid productId, Guid customerId, int take = 10);
}
