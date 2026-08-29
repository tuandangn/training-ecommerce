using NamEcommerce.Domain.Shared.Enums.Customers;

namespace NamEcommerce.Domain.Shared.Dtos.Customers;

[Serializable]
public sealed record CustomerDto(Guid Id)
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Address { get; init; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    public CustomerKind Kind { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}