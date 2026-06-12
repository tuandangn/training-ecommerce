using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Customers;

[Serializable]
public sealed class MatchedPhoneCustomerSpec(string keywords)
    : BaseSpecification<Customer>(customer => customer.PhoneNumber.Contains(keywords));
