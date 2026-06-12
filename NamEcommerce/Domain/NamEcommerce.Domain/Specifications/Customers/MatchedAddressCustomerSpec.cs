using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Filters;

namespace NamEcommerce.Domain.Specifications.Customers;

[Serializable]
public sealed class MatchedAddressCustomerSpec(KeywordFilter filter)
    : BaseSpecification<Customer>(customer => customer.Address.Value.ToUpper().Contains(filter.UppercaseKeywords)
        || customer.Address.Value.ToUpper().Contains(filter.NormalizedKeywords) 
        || customer.Address.NormalizedValue.Contains(filter.NormalizedKeywords));
