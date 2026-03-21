using System.Text.RegularExpressions;

namespace NamEcommerce.Application.Contracts.Dtos.Catalog;

[Serializable]
public abstract record BaseVendorAppDto
{
    public required string Name { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Address { get; set; }
    public int DisplayOrder { get; set; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Name))
            return (false, "Name cannot be empty");
        if (string.IsNullOrEmpty(PhoneNumber))
            return (false, "Phone nummber cannot be empty.");
        if (!Regex.IsMatch(PhoneNumber, @"0\d{9,10}"))
            return (false, "Phone number is invalid");
        return (true, string.Empty);
    }
}

[Serializable]
public sealed record VendorAppDto(Guid Id) : BaseVendorAppDto;

[Serializable]
public sealed record CreateVendorAppDto : BaseVendorAppDto;
[Serializable]
public sealed record CreateVendorResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdateVendorAppDto(Guid Id) : BaseUnitMeasurementAppDto;
[Serializable]
public sealed record UpdateVendorResultAppDto
{
    public required bool Success { get; init; }
    public Guid UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record DeleteVendorAppDto(Guid Id);

[Serializable]
public sealed record DeleteVendorResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}
