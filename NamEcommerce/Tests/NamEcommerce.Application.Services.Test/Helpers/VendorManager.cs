using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class VendorManager
{
    public static Mock<IVendorManager> WhenGetVendorsReturns(string keywords, int pageIndex, int pageSize, IPagedDataDto<VendorDto> @return)
    {
        var mock = new Mock<IVendorManager>();
        mock.Setup(r => r.GetVendorsAsync(keywords, pageIndex, pageSize)).ReturnsAsync(@return).Verifiable();

        return mock;
    }

    public static Mock<IVendorManager> SetUsernameExists(string name, bool exists)
    {
        var mock = new Mock<IVendorManager>();
        mock.Setup(r => r.DoesNameExistAsync(name, null)).ReturnsAsync(exists).Verifiable();

        return mock;
    }

    public static Mock<IVendorManager> SetUsernameExists(string name, Guid compareId, bool exists)
    {
        var mock = new Mock<IVendorManager>();
        mock.Setup(r => r.DoesNameExistAsync(name, compareId)).ReturnsAsync(exists).Verifiable();

        return mock;
    }

    public static Mock<IVendorManager> CreateVendorReturns(this Mock<IVendorManager> mock, CreateVendorDto dto, CreateVendorResultDto @return)
    {
        mock.Setup(r => r.CreateVendorAsync(dto)).ReturnsAsync(@return).Verifiable();

        return mock;
    }

    public static Mock<IVendorManager> UpdateVendorReturns(this Mock<IVendorManager> mock, UpdateVendorDto dto, UpdateVendorResultDto @return)
    {
        mock.Setup(r => r.UpdateVendorAsync(dto)).ReturnsAsync(@return).Verifiable();

        return mock;
    }

    public static Mock<IVendorManager> CanDeleteVendor(Guid id)
    {
        var mock = new Mock<IVendorManager>();

        mock.Setup(r => r.DeleteVendorAsync(id)).Returns(Task.CompletedTask).Verifiable();

        return mock;
    }
}
