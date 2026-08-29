using NamEcommerce.Domain.Shared.Enums.Customers;

namespace NamEcommerce.Domain.Shared.Dtos.Customers;

[Serializable]
public sealed record UpdateCustomerDto(Guid Id)
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Address { get; init; }
    public required CustomerKind Kind { get; init; }
    public string? Email { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record UpdateCustomerResultDto
{
    public required Guid UpdatedId { get; init; }
}
