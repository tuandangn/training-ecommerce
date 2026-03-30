using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Events;
using System.Linq.Expressions;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class VendorManagerTests
{
    #region CreateVendorAsync

    [Fact]
    public async Task CreateVendorAsync_DtoIsNull_ThrowArgumentNullException()
    {
        var vendorManager = new VendorManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => vendorManager.CreateVendorAsync(null!));
    }

    [Fact]
    public async Task CreateVendorAsync_DataIsInvalid_ThrowsVendorDataIsInvalidException()
    {
        var invalidCreateVendorDto = new CreateVendorDto
        {
            Name = string.Empty,
            PhoneNumber = "invalid-phone", //or empty
        };
        var vendorManager = new VendorManager(null!, null!, null!);

        await Assert.ThrowsAsync<VendorDataIsInvalidException>(() => vendorManager.CreateVendorAsync(invalidCreateVendorDto));
    }

    [Fact]
    public async Task CreateVendorAsync_NameIsExists_ThrowsVendorNameExistsException()
    {
        var existingName = "existing-name";
        var dto = new CreateVendorDto
        {
            Name = existingName,
            PhoneNumber = "0123456789"
        };
        var vendorDataReaderMock = VendorDataReader.HasOne(new Vendor(Guid.NewGuid(), existingName, "phoneNumber"));
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<VendorNameExistsException>(() => vendorManager.CreateVendorAsync(dto));
    }

    [Fact]
    public async Task CreateVendorAsync_DataIsValid_ReturnsCreatedVendor()
    {
        var dto = new CreateVendorDto
        {
            Name = "vendor-name",
            PhoneNumber = "0123456789"
        };
        var returnVendor = new Vendor(Guid.NewGuid(), dto.Name, dto.PhoneNumber);
        var vendorRepositoryMock = VendorRepository.CreateVendorWillReturns(returnVendor);
        var vendorDataReaderStub = VendorDataReader.Empty();
        var vendorManager = new VendorManager(vendorRepositoryMock.Object, vendorDataReaderStub.Object, Mock.Of<IEventPublisher>());

        var vendorDto = await vendorManager.CreateVendorAsync(dto);

        Assert.Equal(returnVendor.Id, vendorDto.CreatedId);
        vendorRepositoryMock.Verify();
    }

    #endregion

    #region DoesNameExistAsync

    [Fact]
    public async Task DoesNameExistAsync_NameIsNull_ThrowsArgumentNullException()
    {
        var vendorManager = new VendorManager(null!, null!, null!);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            vendorManager.DoesNameExistAsync(null!)
        );
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdEquals_ReturnsFalse()
    {
        var hasNameVendorId = Guid.NewGuid();
        var testName = "test-name-existing";
        var vendorDataReaderMock = VendorDataReader.HasOne(new Vendor(hasNameVendorId, testName, "phoneNumber"));
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object, null!);

        var nameExists = await vendorManager.DoesNameExistAsync(testName, comparesWithCurrentId: hasNameVendorId);

        Assert.False(nameExists);
        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdIsNotProvided_ReturnsTrue()
    {
        var testName = "test-name-existing";
        var vendorDataReaderMock = VendorDataReader.HasOne(new Vendor(default, testName, "phoneNumber"));
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object, null!);

        var nameExists = await vendorManager.DoesNameExistAsync(testName, comparesWithCurrentId: null);

        Assert.True(nameExists);
        vendorDataReaderMock.Verify();
    }

    #endregion

    #region UpdateVendorAsync

    [Fact]
    public async Task UpdateVendorAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var vendorManager = new VendorManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => vendorManager.UpdateVendorAsync(null!));
    }

    [Fact]
    public async Task UpdateVendorAsync_DataIsInvalid_ThrowsVendorDataIsInvalidException()
    {
        var vendorManager = new VendorManager(null!, null!, null!);

        await Assert.ThrowsAsync<VendorDataIsInvalidException>(() =>
            vendorManager.UpdateVendorAsync(new UpdateVendorDto(Guid.NewGuid())
            {
                Name = string.Empty,
                PhoneNumber = string.Empty
            })
        );
    }

    [Fact]
    public async Task UpdateVendorAsync_VendorIsNotFound_ThrowsArgumentException()
    {
        var notFoundVendorId = Guid.NewGuid();
        var vendorDataReaderMock = VendorDataReader.NotFound(notFoundVendorId);
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => vendorManager.UpdateVendorAsync(new UpdateVendorDto(notFoundVendorId)
            {
                Name = "vendor",
                PhoneNumber = "0123456789"
            }));
        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateVendorAsync_VendorNameIsExists_ThrowsVendorNameExistsException()
    {
        var oldVendor = new Vendor(Guid.NewGuid(), "old-vendor-name", "0123456789");
        var updateVendor = new Vendor(oldVendor.Id, "new-vendor-name", "0123456789");
        var sameNameCategoryId = Guid.NewGuid();
        var vendorDataReaderMock = VendorDataReader
            .HasOne(new Vendor(default, updateVendor.Name, "0123456789"))
            .VendorById(oldVendor.Id, oldVendor);
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<VendorNameExistsException>(()
            => vendorManager.UpdateVendorAsync(new UpdateVendorDto(updateVendor.Id)
            {
                Name = updateVendor.Name,
                PhoneNumber = updateVendor.PhoneNumber
            }));

        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateVendorAsync_UpdateVendor()
    {
        var oldVendor = new Vendor(Guid.NewGuid(), "old-vendor-name", "0123456789")
        {
            DisplayOrder = 1
        };
        var updateVendor = new Vendor(oldVendor.Id, "new-vendor-name", "0123456789")
        {
            DisplayOrder = 2
        };
        Expression<Func<Vendor, bool>> isVendorMatch =
            c => c.Id == updateVendor.Id
                && c.Name == updateVendor.Name
                && c.DisplayOrder == updateVendor.DisplayOrder;
        var vendorRepositoryMock = Repository.Create<Vendor>()
            .WhenCall(repository => repository.UpdateAsync(It.Is(isVendorMatch), default), updateVendor);
        var vendorDataReaderStub = VendorDataReader.VendorById(oldVendor.Id, oldVendor);
        var vendorManager = new VendorManager(vendorRepositoryMock.Object, vendorDataReaderStub.Object, Mock.Of<IEventPublisher>());

        var resultVendor = await vendorManager.UpdateVendorAsync(
            new UpdateVendorDto(updateVendor.Id)
            {
                Name = updateVendor.Name,
                PhoneNumber = updateVendor.PhoneNumber,
                DisplayOrder = updateVendor.DisplayOrder,
            });

        Assert.Equal(resultVendor, resultVendor with
        {
            Id = updateVendor.Id,
            Name = updateVendor.Name,
            DisplayOrder = updateVendor.DisplayOrder
        });
        vendorRepositoryMock.Verify();
    }

    #endregion

    #region DeleteVendorAsync

    [Fact]
    public async Task DeleteVendorAsync_VendorIsNotFound_ThrowsArgumentException()
    {
        var notFoundVendorId = Guid.NewGuid();
        var vendorDataReaderMock = VendorDataReader.NotFound(notFoundVendorId);
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => vendorManager.DeleteVendorAsync(notFoundVendorId));

        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteVendorAsync_DeleteVendor()
    {
        var vendor = new Vendor(Guid.NewGuid(), "vendor", "013456789");
        var vendorDataRepositoryMock = VendorRepository.CanDeleteVendor(vendor);
        var vendorDataReaderMock = VendorDataReader.VendorById(vendor.Id, vendor);
        var vendorManager = new VendorManager(vendorDataRepositoryMock.Object, vendorDataReaderMock.Object, Mock.Of<IEventPublisher>());

        await vendorManager.DeleteVendorAsync(vendor.Id);

        vendorDataReaderMock.Verify();
    }

    #endregion

    #region GetVendorsAsync

    [Fact]
    public async Task GetVendorsAsync_PageIndexLessThanZero_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageIndex = -1;
        var vendorManager = new VendorManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            vendorManager.GetVendorsAsync("keywords", invalidPageIndex, int.MaxValue));
    }

    [Fact]
    public async Task GetVendorsAsync_PageSizeLessThanOrEqualZero_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageSize = 0;
        var vendorManager = new VendorManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            vendorManager.GetVendorsAsync("keywords", 0, invalidPageSize));
    }

    [Fact]
    public async Task GetVendorsAsync_KeywordsIsEmpty_ReturnPagedOrderedData()
    {
        var pageIndex = 0;
        var pageSize = 2;
        var vendor1 = new Vendor(Guid.NewGuid(), "vendor-1", "013456789")
        {
            DisplayOrder = 2
        };
        var vendor2 = new Vendor(Guid.NewGuid(), "vendor-2", "013456789")
        {
            DisplayOrder = 1 //first
        };
        var vendor3 = new Vendor(Guid.NewGuid(), "vendor-3", "013456789")
        {
            DisplayOrder = 1 //second
        };
        var vendorDataReaderMock = VendorDataReader.WithData(vendor1, vendor2, vendor3);
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object, null!);

        var pagedOrderedResult = await vendorManager.GetVendorsAsync("", pageIndex, pageSize);

        Assert.Equal(3, pagedOrderedResult.PagerInfo.TotalCount);
        Assert.Equal(vendor2.Id, pagedOrderedResult.ElementAt(0).Id);
        Assert.Equal(vendor3.Id, pagedOrderedResult.ElementAt(1).Id);
        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetVendorsAsync_KeywordsIsIncluded_ReturnFilteredData()
    {
        var keywords = "keywords";
        var pageIndex = 1; //second page
        var pageSize = 1;
        var vendor1 = new Vendor(Guid.NewGuid(), "keywords-1", "013456789");
        var vendor2 = new Vendor(Guid.NewGuid(), "keywords-2", "013456789");
        var vendor3 = new Vendor(Guid.NewGuid(), "vendor", "013456789");
        var vendorDataReaderMock = VendorDataReader.WithData(vendor1, vendor2, vendor3);
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object, null!);

        var filteredResult = await vendorManager.GetVendorsAsync(keywords, pageIndex, pageSize);

        Assert.Equal(2, filteredResult.PagerInfo.TotalCount);
        Assert.Equal(vendor2.Id, filteredResult.ElementAt(0).Id);
        vendorDataReaderMock.Verify();
    }

    #endregion
}
