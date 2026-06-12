using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class MatchedCodeOrderSpec(string keywords) : BaseSpecification<Order>(order => order.Code.Contains(keywords));

