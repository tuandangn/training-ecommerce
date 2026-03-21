using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Services.Extensions;

public static class VendorExtensions
{
    public static VendorDto ToDto(this Vendor vendor)
        => new VendorDto(vendor.Id)
        {
            Name = vendor.Name,
            PhoneNumber = vendor.PhoneNumber,
            Address = vendor.Address,
            DisplayOrder = vendor.DisplayOrder
        };
}
