using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class OrderKeywordSearchSpec(string keywords) : BaseSpecification<Order>(
    new MatchedCodeOrderSpec(keywords).Criteria
    .Or(new MatchedShippingAddressOrderSpec(keywords).Criteria)
    .Or(new MatchedShippingPhoneOrderSpec(keywords).Criteria)
);

