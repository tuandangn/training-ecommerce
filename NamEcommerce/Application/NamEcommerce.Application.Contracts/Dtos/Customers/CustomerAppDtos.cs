using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Customers;

[Serializable]
public sealed record CustomerAppDto(Guid Id)
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Address { get; init; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

[Serializable]
public sealed record CreateCustomerAppDto
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Address { get; init; }
    public string? Email { get; set; }
    public string? Note { get; set; }
}

[Serializable]
public sealed record UpdateCustomerAppDto(Guid Id)
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Address { get; init; }
    public string? Email { get; set; }
    public string? Note { get; set; }
}

[Serializable]
public sealed record CreateCustomerResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record UpdateCustomerResultAppDto
{
    public required bool Success { get; init; }
    public Guid? UpdatedId { get; init; }
    public string? ErrorMessage { get; init; }
}

[Serializable]
public sealed record DeleteCustomerResultAppDto
{
    public required bool Success { get; init; }
    public Guid? DeletedId { get; init; }
    public string? ErrorMessage { get; init; }
}
