using NamEcommerce.Domain.Shared.Enums.Customers;

namespace NamEcommerce.Domain.Shared.Dtos.Customers;

[Serializable]
public sealed record ChangeCustomerKindDto
{
    public required Guid CustomerId { get; init; }
    public required CustomerKind Kind { get; init; }
}
