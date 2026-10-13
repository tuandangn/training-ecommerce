using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class OrdersOfOrderItemsSpec(IList<Guid> orderItemIds)
    : BaseSpecification<Order>(order => order.OrderItems.Any(item => orderItemIds.Contains(item.Id)));
