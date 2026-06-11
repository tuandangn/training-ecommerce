using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class OrdersOfCreatorsSpec(Guid?[] userIds) : BaseSpecification<Order>(order => userIds.Contains(order.CreatedByUserId));
