using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Customers;

[Serializable]
public sealed class HaveKindCustomerSpec(CustomerKind kind)
    : BaseSpecification<Customer>(customer => customer.Kind == kind);
