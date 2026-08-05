using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Customers;

[Serializable]
public sealed class IsSystemCustomerSpec(bool isSystem) : BaseSpecification<Customer>(
    customer => customer.IsSystem == isSystem);
