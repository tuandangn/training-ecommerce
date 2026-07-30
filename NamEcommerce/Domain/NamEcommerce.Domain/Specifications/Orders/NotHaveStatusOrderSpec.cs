using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class NotHaveStatusOrderSpec(IEnumerable<OrderStatus> status) : BaseSpecification<Order>(order => !status.Contains(order.OrderStatus));
