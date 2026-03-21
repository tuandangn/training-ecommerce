using NamEcommerce.Domain.Services.Test.Helpers;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class VendorManagerTests
{
    #region CreateVendorAsync

    [Fact]
    public async Task CreateVendorAsync_DtoIsNull_ThrowArgumentNullException()
    {
        var vendorManager = new VendorManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => vendorManager.CreateVendorAsync(null!));
    }

    [Fact]
    public async Task CreateVendorAsync_DataIsInvalid_ThrowsArgumentException()
    {
        var invalidCreateVendorDto = new CreateVendorDto
        {
            Name = "",
            PhoneNumber = "invalid-phone", //or empty
        };
        var vendorManager = new VendorManager(null!, null!);

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
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object);

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
        var vendorManager = new VendorManager(vendorRepositoryMock.Object, vendorDataReaderStub.Object);

        var vendorDto = await vendorManager.CreateVendorAsync(new CreateVendorDto
        {
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber
        });

        Assert.Equal(returnVendor.Id, vendorDto.CreatedId);
        vendorRepositoryMock.Verify();
    }

    #endregion

    #region DoesNameExistAsync

    [Fact]
    public async Task DoesNameExistAsync_NameIsNull_ThrowsArgumentNullException()
    {
        var vendorManager = new VendorManager(null!, null!);

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
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object);

        var nameExists = await vendorManager.DoesNameExistAsync(testName, comparesWithCurrentId: hasNameVendorId);

        Assert.False(nameExists);
        vendorDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdIsNotProvided_ReturnsTrue()
    {
        var testName = "test-name-existing";
        var vendorDataReaderMock = VendorDataReader.HasOne(new Vendor(default, testName, "phoneNumber"));
        var vendorManager = new VendorManager(null!, vendorDataReaderMock.Object);

        var nameExists = await vendorManager.DoesNameExistAsync(testName, comparesWithCurrentId: null);

        Assert.True(nameExists);
        vendorDataReaderMock.Verify();
    }

    #endregion
}
