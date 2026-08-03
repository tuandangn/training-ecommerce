using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Filters;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class MatchedShippingAddressOrderSpec(KeywordFilter filter)
    : BaseSpecification<Order>(order => order.ShippingAddress.Value.ToUpper().Contains(filter.UppercaseKeywords)
        || order.ShippingAddress.Value.ToUpper().Contains(filter.NormalizedKeywords)
        || order.ShippingAddress.NormalizedValue.Contains(filter.NormalizedKeywords));
