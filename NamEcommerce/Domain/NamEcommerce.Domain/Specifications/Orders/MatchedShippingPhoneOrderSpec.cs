using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class MatchedShippingPhoneOrderSpec(string keywords)
    : BaseSpecification<Order>(order => order.ShippingPhoneNumber != null && order.ShippingPhoneNumber.Contains(keywords));
