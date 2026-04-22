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
            throw new VendorDataIsInvalidException("Tên nhà cung cấp không được để trống");
        if (string.IsNullOrEmpty(PhoneNumber))
            throw new VendorDataIsInvalidException("Số điện thoại nhà cung cấp không được để trống");
        if (!Regex.IsMatch(PhoneNumber, @"0\d{9,10}"))
            throw new VendorDataIsInvalidException("Số điện thoại nhà cung cấp không hợp lệ");
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

