using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Filters;

namespace NamEcommerce.Domain.Specifications.Customers;

[Serializable]
public sealed class MatchedNameCustomerSpec(KeywordFilter filter)
    : BaseSpecification<Customer>(customer => customer.FullName.Value.ToUpper().Contains(filter.UppercaseKeywords)
        || customer.FullName.Value.ToUpper().Contains(filter.NormalizedKeywords) 
        || customer.FullName.NormalizedValue.Contains(filter.NormalizedKeywords));
