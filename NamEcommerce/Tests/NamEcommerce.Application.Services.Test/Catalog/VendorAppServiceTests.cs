using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Services.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Application.Services.Test.Catalog;

public sealed class VendorAppServiceTests
{

    #region GetVendorsAsync

    [Fact]
    public async Task GetVendorsAsync_KeywordIsNotNull_ReturnsOrderedPagedData()
    {
        var keyword = "keyword";
        var pageIndex = 0;
        var pageSize = int.MaxValue;
        var pagedData = PagedDataDto.Create(
            [new VendorDto(Guid.NewGuid()) { Name = $"{keyword}-1", DisplayOrder = 1, PhoneNumber = "0123456789" },
            new VendorDto(Guid.NewGuid()) { Name = $"{keyword}-2", DisplayOrder = 2, PhoneNumber = "0123456789" }],
            pageIndex, pageSize
        );
        var vendorManagerMock = VendorManager.WhenGetVendorsReturns(keyword, pageIndex, pageSize, pagedData);
        var vendorAppService = new VendorAppService(vendorManagerMock.Object, null!);

        var pagedDataResult = await vendorAppService.GetVendorsAsync(keyword, 0, int.MaxValue);

        Assert.Equal(pagedData.ElementAt(0).Id, pagedDataResult.Items.First().Id);
        Assert.Equal(pagedData.ElementAt(1).Id, pagedDataResult.Items.ElementAt(1).Id);
        Assert.Equal(2, pagedDataResult.Pagination.TotalCount);
        vendorManagerMock.Verify();
    }

    #endregion

    #region GetVendorByIdAsync

    [Fact]
    public async Task GetVendorByIdAsync_VendorNotFound_ReturnsNull()
    {
        var notFoundId = Guid.NewGuid();
        var vendorDataReaderMock = VendorDataReader.NotFound(notFoundId);
        var vendorAppService = new VendorAppService(null!, vendorDataReaderMock.Object);

        var result = await vendorAppService.GetVendorByIdAsync(notFoundId);

        Assert.Null(result);
        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetVendorByIdAsync_VendorFound_ReturnsData()
    {
        var findId = Guid.NewGuid();
        var vendor = new Vendor(findId, "vendor", "0123456789");
        var vendorDataReaderMock = VendorDataReader.VendorById(vendor.Id, vendor);
        var vendorAppService = new VendorAppService(null!, vendorDataReaderMock.Object);

        var result = await vendorAppService.GetVendorByIdAsync(findId);

        Assert.Equal(vendor.Id, result!.Id);
        vendorDataReaderMock.Verify();
    }

    #endregion

    #region CreateVendorAsync

    [Fact]
    public async Task CreateVendorAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var vendorAppService = new VendorAppService(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => vendorAppService.CreateVendorAsync(null!));
    }

    [Fact]
    public async Task CreateVendorAsync_DtoIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new CreateVendorAppDto
        {
            Name = string.Empty,
            PhoneNumber = string.Empty,
            DisplayOrder = 0
        };
        var vendorAppService = new VendorAppService(null!, null!);
        var falseResult = await vendorAppService.CreateVendorAsync(invalidDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
    }

    [Fact]
    public async Task CreateVendorAsync_NameIsExists_ThrowsVendorNameExistsException()
    {
        var existingName = "existing-name";
        var existingNameDto = new CreateVendorAppDto
        {
            Name = existingName,
            PhoneNumber = "0123456789",
            DisplayOrder = 0
        };
        var vendorManager = VendorManager.SetUsernameExists(existingName, true);
        var vendorAppService = new VendorAppService(vendorManager.Object, null!);

        var falseResult = await vendorAppService.CreateVendorAsync(existingNameDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        vendorManager.Verify();
    }

    [Fact]
    public async Task CreateVendorAsync_NameIsNotExists_ReturnsResult()
    {
        var dto = new CreateVendorAppDto
        {
            Name = "new-unit-measurement",
            PhoneNumber = "0123456789",
            DisplayOrder = 0
        };
        var createVendorResult = new CreateVendorResultDto
        {
            CreatedId = Guid.NewGuid()
        };
        var vendorManager = VendorManager.SetUsernameExists(dto.Name, false)
            .CreateVendorReturns(new CreateVendorDto
            {
                Name = dto.Name,
                PhoneNumber = "0123456789",
                DisplayOrder = dto.DisplayOrder
            }, createVendorResult);
        var vendorAppService = new VendorAppService(vendorManager.Object, null!);

        var result = await vendorAppService.CreateVendorAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(createVendorResult.CreatedId, result.CreatedId);
        vendorManager.Verify();
    }

    #endregion
}
