using System.Text.RegularExpressions;

namespace NamEcommerce.Application.Contracts.Dtos.Inventory;

[Serializable]
public abstract record BaseWarehouseAppDto
{
    public required string Code { get; init; }
    public required string Name { get; init; }

    public int WarehouseType { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Guid? ManagerUserId { get; set; }

    public bool IsActive { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Code))
            return (false, "Error.WarehouseCodeRequired");
        if (string.IsNullOrEmpty(Name))
            return (false, "Error.WarehouseNameRequired");
        if (!string.IsNullOrEmpty(PhoneNumber) && !Regex.IsMatch(PhoneNumber, @"0\d{9,10}"))
            return (false, "Error.PhoneNumberInvalid");
        return (true, string.Empty);
    }
}

[Serializable]
public sealed record WarehouseAppDto(Guid Id) : BaseWarehouseAppDto
{
    public string? WarehouseNameKey { get; set; }
}

[Serializable]
public sealed record CreateWarehouseAppDto : BaseWarehouseAppDto;
[Serializable]
public sealed record CreateWarehouseResultAppDto
{
    public required bool Success { get; init; }
    public Guid CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdateWarehouseAppDto(Guid Id) : BaseWarehouseAppDto;
[Serializable]
public sealed record UpdateWarehouseResultAppDto
{
    public required bool Success { get; init; }
    public Guid UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record DeleteWarehouseAppDto(Guid Id);

[Serializable]
public sealed record DeleteWarehouseResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}
