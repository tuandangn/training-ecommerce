using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class OrdersOfBuyersSpec(Guid?[] customerIds) : BaseSpecification<Order>(order => customerIds.Contains(order.CustomerId));
