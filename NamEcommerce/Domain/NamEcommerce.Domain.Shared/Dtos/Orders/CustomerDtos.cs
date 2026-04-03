namespace NamEcommerce.Domain.Shared.Dtos.Orders;

[Serializable]
public sealed record CustomerDto(Guid Id)
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateCustomerDto
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record UpdateCustomerDto(Guid Id)
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed record CreateCustomerResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateCustomerResultDto
{
    public required Guid UpdatedId { get; init; }
}
