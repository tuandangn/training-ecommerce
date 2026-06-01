using NamEcommerce.Domain.Shared.Dtos.Orders;

namespace NamEcommerce.Domain.Shared.Services.Orders;

public interface IOrderItemChangeAuditManager
{
    Task RecordAsync(CreateOrderItemChangeAuditDto dto);
    Task<IReadOnlyList<OrderItemChangeAuditDto>> GetByOrderIdAsync(Guid orderId);
}
