using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using System.Text.RegularExpressions;

namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public abstract record BaseVendorDto
{
    public required string Name { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Address { get; set; }
    public int DisplayOrder { get; set; }

    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Name))
            throw new VendorDataIsInvalidException("Error.VendorNameRequired");
        if (string.IsNullOrEmpty(PhoneNumber))
            throw new VendorDataIsInvalidException("Error.VendorPhoneNumberRequired");
        if (!Regex.IsMatch(PhoneNumber, @"0\d{9,10}"))
            throw new VendorDataIsInvalidException("Error.PhoneNumberInvalid");
    }
}

[Serializable]
public sealed record VendorDto(Guid Id) : BaseVendorDto;

[Serializable]
public sealed record CreateVendorDto : BaseVendorDto;
[Serializable]
public sealed record CreateVendorResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateVendorDto(Guid Id) : BaseVendorDto;
[Serializable]
public sealed record UpdateVendorResultDto(Guid Id) : BaseVendorDto;

