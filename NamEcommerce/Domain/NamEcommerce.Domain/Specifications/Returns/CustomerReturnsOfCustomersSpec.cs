using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Returns;

[Serializable]
public sealed class CustomerReturnsOfCustomersSpec(IList<Guid> customerIds)
    : BaseSpecification<CustomerReturn>(customerReturn => customerIds.Contains(customerReturn.CustomerId));
