using NamEcommerce.Application.Contracts.Dtos.Orders;

namespace NamEcommerce.Application.Contracts.Orders;

public interface IOrderAuditAppService
{
    Task<IReadOnlyList<OrderItemChangeAuditAppDto>> GetOrderItemChangeAuditsAsync(Guid orderId);
}
