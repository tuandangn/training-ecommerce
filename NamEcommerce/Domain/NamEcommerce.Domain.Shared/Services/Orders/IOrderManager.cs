using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Services.Orders;

public interface IOrderManager : ICodeExistCheckingService
{
    Task<OrderDto?> GetOrderByIdAsync(Guid id);
    Task<IPagedDataDto<OrderDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords = null, OrderStatus? status = null, OrderStatus? notStatus = null);
    Task<IPagedDataDto<OrderDto>> GetOrdersAsync(int pageIndex, int pageSize, string? keywords = null, IEnumerable<OrderStatus>? status = null, IEnumerable<OrderStatus>? notStatus = null);

    Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderDto dto);
    Task<UpdateOrderResultDto> UpdateOrderAsync(UpdateOrderDto dto);
    Task DeleteOrderAsync(DeleteOrderDto dto);

    Task UpdateShippingAsync(UpdateShippingDto dto);
    Task MarkOrderHasPayment(Guid orderId, decimal paidAmount, Guid? paymentIntentId);
    Task RequestDeliveryAsync(Guid orderId, Guid deliveryNoteId, DateTime requestedAtUtc);

    Task CompleteOrderAsync(CompleteOrderDto dto);
    Task CancelOrderAsync(CancelOrderDto dto);

    Task AddOrderItemAsync(Guid orderId, AddOrderItemDto dto);
    Task UpdateOrderItemAsync(UpdateOrderItemDto dto);
    Task DeleteOrderItemAsync(DeleteOrderItemDto dto);

    Task MarkOrderItemDeliveredAsync(MarkOrderItemDeliveredDto dto);
    Task MarkOrderItemReceivedByCustomerAsync(MarkOrderItemReceivedByCustomerDto dto);

    Task<IList<RecentSalePriceDto>> GetRecentSalePricesAsync(Guid productId, Guid customerId, int take = 10);
}
