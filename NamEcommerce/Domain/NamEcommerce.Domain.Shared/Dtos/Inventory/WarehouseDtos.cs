using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using System.Text.RegularExpressions;

namespace NamEcommerce.Domain.Shared.Dtos.Inventory;

[Serializable]
public abstract record BaseWarehouseDto
{
    public required string Code { get; init; }
    public required string Name { get; init; }

    public int WarehouseType { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Guid? ManagerUserId { get; set; }
    public bool IsActive { get; set; }

    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Code))
            throw new WarehouseDataIsInvalidException("Code is not empty");
        if (string.IsNullOrEmpty(Name))
            throw new WarehouseDataIsInvalidException("Name is not empty");
        if (!string.IsNullOrEmpty(PhoneNumber) && !Regex.IsMatch(PhoneNumber, @"0\d{9,10}"))
            throw new WarehouseDataIsInvalidException("Warehouse phone number is invalid");
    }
}

[Serializable]
public sealed record WarehouseDto(Guid Id) : BaseWarehouseDto
{
    public string? WarehouseNameKey { get; set; }
}

[Serializable]
public sealed record CreateWarehouseDto : BaseWarehouseDto;
[Serializable]
public sealed record CreateWarehouseResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateWarehouseDto(Guid Id) : BaseWarehouseDto;
[Serializable]
public sealed record UpdateWarehouseResultDto(Guid Id) : BaseWarehouseDto;
