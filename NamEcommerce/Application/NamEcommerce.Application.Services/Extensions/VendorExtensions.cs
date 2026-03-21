using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Services.Extensions;

public static class VendorExtensions
{
    public static VendorAppDto ToDto(this Vendor vendor)
        => new VendorAppDto(vendor.Id)
        {
            Name = vendor.Name,
            PhoneNumber = vendor.PhoneNumber,
            Address = vendor.Address,
            DisplayOrder = vendor.DisplayOrder
        };

    public static VendorAppDto ToDto(this VendorDto dto)
        => new VendorAppDto(dto.Id)
        {
            Name = dto.Name,
            PhoneNumber= dto.PhoneNumber,
            Address= dto.Address,
            DisplayOrder = dto.DisplayOrder
        };
}
