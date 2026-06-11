using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Filters;

namespace NamEcommerce.Domain.Specifications.Customers;

[Serializable]
public sealed class CustomerKeywordSearchSpec(KeywordFilter filter)
    : BaseSpecification<Customer>(
        new MatchedNameCustomerSpec(filter).Criteria
        .Or(new MatchedAddressCustomerSpec(filter).Criteria)
        .Or(new MatchedPhoneCustomerSpec(filter.Keywords).Criteria));

