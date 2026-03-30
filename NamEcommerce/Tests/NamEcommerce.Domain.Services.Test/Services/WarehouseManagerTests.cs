using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Services.Inventory;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class WarehouseManagerTests
{
    #region CreateWarehouseAsync

    [Fact]
    public async Task CreateWarehouseAsync_DtoIsNull_ThrowArgumentNullException()
    {
        var warehouseManager = new WarehouseManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => warehouseManager.CreateWarehouseAsync(null!));
    }

    [Fact]
    public async Task CreateWarehouseAsync_DataIsInvalid_ThrowsWarehouseDataIsInvalidException()
    {
        var invalidCreateWarehouseDto = new CreateWarehouseDto
        {
            Code = string.Empty,
            Name = string.Empty,
            PhoneNumber = "invalid-phone",
        };
        var warehouseManager = new WarehouseManager(null!, null!, null!);

        await Assert.ThrowsAsync<WarehouseDataIsInvalidException>(() => warehouseManager.CreateWarehouseAsync(invalidCreateWarehouseDto));
    }

    [Fact]
    public async Task CreateWarehouseAsync_NameIsExists_ThrowsWarehouseNameExistsException()
    {
        var existingName = "existing-name";
        var dto = new CreateWarehouseDto
        {
            Code = "code",
            Name = existingName
        };
        var warehouseDataReaderMock = WarehouseDataReader.HasOne(new Warehouse("code-code", existingName, (WarehouseType)dto.WarehouseType));
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<WarehouseNameExistsException>(() => warehouseManager.CreateWarehouseAsync(dto));
    }

    [Fact]
    public async Task CreateWarehouseAsync_CodeIsExists_ThrowsWarehouseCodeExistsException()
    {
        var existingCode = "existing-code";
        var dto = new CreateWarehouseDto
        {
            Code = existingCode,
            Name = "warehouse-name"
        };
        var warehouseDataReaderMock = WarehouseDataReader.HasOne(new Warehouse(existingCode, "warehouse-name-1", (WarehouseType)dto.WarehouseType));
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<WarehouseCodeExistsException>(() => warehouseManager.CreateWarehouseAsync(dto));
    }

    [Fact]
    public async Task CreateWarehouseAsync_DataIsValid_ReturnsCreatedWarehouse()
    {
        var dto = new CreateWarehouseDto
        {
            Code = "code",
            Name = "warehouse-name",
            Address = "warehouse-address",
            PhoneNumber = "0123456789",
            ManagerUserId = Guid.NewGuid(),
            WarehouseType = (int)WarehouseType.Main
        };
        var returnWarehouse = new Warehouse(dto.Code, dto.Name, (WarehouseType)dto.WarehouseType)
        {
            Address = dto.Address,
            PhoneNumber = dto.PhoneNumber,
            ManagerUserId = dto.ManagerUserId,
        };
        var warehouseRepositoryMock = WarehouseRepository.CreateWarehouseWillReturns(returnWarehouse);
        var warehouseDataReaderStub = WarehouseDataReader.Empty();
        var warehouseManager = new WarehouseManager(warehouseRepositoryMock.Object, warehouseDataReaderStub.Object, Mock.Of<IEventPublisher>());

        var warehouseDto = await warehouseManager.CreateWarehouseAsync(dto);

        Assert.Equal(returnWarehouse.Id, warehouseDto.CreatedId);
        warehouseRepositoryMock.Verify();
    }

    #endregion

    #region DoesNameExistAsync

    [Fact]
    public async Task DoesNameExistAsync_NameIsNull_ThrowsArgumentNullException()
    {
        var warehouseManager = new WarehouseManager(null!, null!, null!);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            warehouseManager.DoesNameExistAsync(null!)
        );
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdEquals_ReturnsFalse()
    {
        var existingName = "test-name-existing";
        var warehouse = new Warehouse("code", existingName, WarehouseType.Main);
        var hasNameWarehouseId = warehouse.Id;
        var warehouseDataReaderMock = WarehouseDataReader.HasOne(warehouse);
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        var nameExists = await warehouseManager.DoesNameExistAsync(existingName, comparesWithCurrentId: hasNameWarehouseId);

        Assert.False(nameExists);
        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdIsNotProvided_ReturnsTrue()
    {
        var testName = "test-name-existing";
        var warehouseDataReaderMock = WarehouseDataReader.HasOne(new Warehouse("code", testName, WarehouseType.Main));
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        var nameExists = await warehouseManager.DoesNameExistAsync(testName, comparesWithCurrentId: null);

        Assert.True(nameExists);
        warehouseDataReaderMock.Verify();
    }

    #endregion

    #region DoesCodeExistAsync

    [Fact]
    public async Task DoesCodeExistAsync_CodeIsNull_ThrowsArgumentNullException()
    {
        var warehouseManager = new WarehouseManager(null!, null!, null!);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            warehouseManager.DoesCodeExistAsync(null!)
        );
    }

    [Fact]
    public async Task DoesCodeExistAsync_CodeIsMatchAndCompareIdEquals_ReturnsFalse()
    {
        var existingCode = "test-code-existing";
        var warehouse = new Warehouse(existingCode, "warehouse-name", WarehouseType.Main);
        var hasCodeWarehouseId = warehouse.Id;
        var warehouseDataReaderMock = WarehouseDataReader.HasOne(warehouse);
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        var codeExists = await warehouseManager.DoesCodeExistAsync(existingCode, comparesWithCurrentId: hasCodeWarehouseId);

        Assert.False(codeExists);
        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesCodeExistAsync_CodeIsMatchAndCompareIdIsNotProvided_ReturnsTrue()
    {
        var testCode = "test-code-existing";
        var warehouseDataReaderMock = WarehouseDataReader.HasOne(new Warehouse(testCode, "warehouse-name", WarehouseType.Main));
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        var codeExists = await warehouseManager.DoesCodeExistAsync(testCode, comparesWithCurrentId: null);

        Assert.True(codeExists);
        warehouseDataReaderMock.Verify();
    }

    #endregion

    #region UpdateWarehouseAsync

    [Fact]
    public async Task UpdateWarehouseAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var warehouseManager = new WarehouseManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => warehouseManager.UpdateWarehouseAsync(null!));
    }

    [Fact]
    public async Task UpdateWarehouseAsync_DataIsInvalid_ThrowsWarehouseDataIsInvalidException()
    {
        var warehouseManager = new WarehouseManager(null!, null!, null!);

        await Assert.ThrowsAsync<WarehouseDataIsInvalidException>(() =>
            warehouseManager.UpdateWarehouseAsync(new UpdateWarehouseDto(Guid.NewGuid())
            {
                Code = string.Empty,
                Name = string.Empty,
                PhoneNumber = "invalid-phone",
            })
        );
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WarehouseIsNotFound_ThrowsArgumentException()
    {
        var notFoundWarehouseId = Guid.NewGuid();
        var warehouseDataReaderMock = WarehouseDataReader.NotFound(notFoundWarehouseId);
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => warehouseManager.UpdateWarehouseAsync(new UpdateWarehouseDto(notFoundWarehouseId)
            {
                Code = "code",
                Name = "warehouse"
            }));
        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WarehouseNameIsExists_ThrowsWarehouseNameExistsException()
    {
        var existingName = "existing-name";
        var oldWarehouse = new Warehouse("old-code", "old-warehouse-name", WarehouseType.Main);
        var warehouseDataReaderMock = WarehouseDataReader
            .HasOne(new Warehouse("code", existingName, WarehouseType.Main))
            .WarehouseById(oldWarehouse.Id, oldWarehouse);
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<WarehouseNameExistsException>(()
            => warehouseManager.UpdateWarehouseAsync(new UpdateWarehouseDto(oldWarehouse.Id)
            {
                Code = "new-code",
                Name = existingName
            }));

        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateWarehouseAsync_WarehouseCodeIsExists_ThrowsWarehouseCodeExistsException()
    {
        var existingCode = "existing-code";
        var oldWarehouse = new Warehouse("old-code", "old-warehouse-name", WarehouseType.Main);
        var warehouseDataReaderMock = WarehouseDataReader
            .HasOne(new Warehouse(existingCode, "warehouse-name", WarehouseType.Main))
            .WarehouseById(oldWarehouse.Id, oldWarehouse);
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<WarehouseCodeExistsException>(()
            => warehouseManager.UpdateWarehouseAsync(new UpdateWarehouseDto(oldWarehouse.Id)
            {
                Code = existingCode,
                Name = "new-warehouse-name"
            }));

        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateWarehouseAsync_UpdateWarehouse()
    {
        var oldWarehouse = new Warehouse("old-code", "old-warehouse-name", WarehouseType.SubWarehouse)
        {
            PhoneNumber = "000000000",
            ManagerUserId = Guid.NewGuid()
        };
        var updatedWarehouse = new Warehouse("new-code", "new-warehouse-name", WarehouseType.SubWarehouse)
        {
            PhoneNumber = "0123456789",
            ManagerUserId = Guid.NewGuid()
        };
        var warehouseRepositoryMock = WarehouseRepository.UpdateWarehouseWillReturns(updatedWarehouse);
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(updatedWarehouse.Id, oldWarehouse);
        var warehouseManager = new WarehouseManager(warehouseRepositoryMock.Object, warehouseDataReaderStub.Object, Mock.Of<IEventPublisher>());

        var resultWarehouse = await warehouseManager.UpdateWarehouseAsync(
            new UpdateWarehouseDto(updatedWarehouse.Id)
            {
                Code = updatedWarehouse.Code,
                Name = updatedWarehouse.Name,
                PhoneNumber = updatedWarehouse.PhoneNumber,
                Address = updatedWarehouse.Address,
                IsActive = updatedWarehouse.IsActive,
                ManagerUserId = updatedWarehouse.ManagerUserId,
                WarehouseType = (int)updatedWarehouse.WarehouseType
            });

        Assert.Equal(resultWarehouse, resultWarehouse with
        {
            Code = updatedWarehouse.Code,
            Id = updatedWarehouse.Id,
            Name = updatedWarehouse.Name,
            IsActive = updatedWarehouse.IsActive,
            ManagerUserId = updatedWarehouse.ManagerUserId,
            WarehouseType = (int)updatedWarehouse.WarehouseType,
            Address = updatedWarehouse.Address,
            PhoneNumber = updatedWarehouse.PhoneNumber
        });
        warehouseRepositoryMock.Verify();
    }

    #endregion

    #region DeleteWarehouseAsync

    [Fact]
    public async Task DeleteWarehouseAsync_WarehouseIsNotFound_ThrowsWarehouseIsNotFoundException()
    {
        var notFoundWarehouseId = Guid.NewGuid();
        var warehouseDataReaderMock = WarehouseDataReader.NotFound(notFoundWarehouseId);
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<WarehouseIsNotFoundException>(()
            => warehouseManager.DeleteWarehouseAsync(notFoundWarehouseId));

        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteWarehouseAsync_DeleteWarehouse()
    {
        var warehouse = new Warehouse("code", "warehouse", WarehouseType.Main);
        var warehouseDataRepositoryMock = WarehouseRepository.CanDeleteWarehouse(warehouse);
        var warehouseDataReaderMock = WarehouseDataReader.WarehouseById(warehouse.Id, warehouse);
        var warehouseManager = new WarehouseManager(warehouseDataRepositoryMock.Object, warehouseDataReaderMock.Object, Mock.Of<IEventPublisher>());

        await warehouseManager.DeleteWarehouseAsync(warehouse.Id);

        warehouseDataReaderMock.Verify();
    }

    #endregion

    #region GetWarehousesAsync

    [Fact]
    public async Task GetWarehousesAsync_PageIndexLessThanZero_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageIndex = -1;
        var warehouseManager = new WarehouseManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            warehouseManager.GetWarehousesAsync("keywords", invalidPageIndex, int.MaxValue));
    }

    [Fact]
    public async Task GetWarehousesAsync_PageSizeLessThanOrEqualZero_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageSize = 0;
        var warehouseManager = new WarehouseManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            warehouseManager.GetWarehousesAsync("keywords", 0, invalidPageSize));
    }

    [Fact]
    public async Task GetWarehousesAsync_KeywordsIsEmpty_ReturnPagedOrderedData()
    {
        var pageIndex = 0;
        var pageSize = 2;
        var warehouse1 = new Warehouse("code-1", "warehouse-1", WarehouseType.Main);
        var warehouse2 = new Warehouse("code-2", "warehouse-2", WarehouseType.SubWarehouse);
        var warehouse3 = new Warehouse("code-3", "warehouse-3", WarehouseType.ReturnWarehouse);
        var warehouseDataReaderMock = WarehouseDataReader.WithData(warehouse1, warehouse2, warehouse3);
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        var pagedOrderedResult = await warehouseManager.GetWarehousesAsync("", pageIndex, pageSize);

        Assert.Equal(3, pagedOrderedResult.PagerInfo.TotalCount);
        Assert.Equal(warehouse1.Id, pagedOrderedResult.ElementAt(0).Id);
        Assert.Equal(warehouse2.Id, pagedOrderedResult.ElementAt(1).Id);
        warehouseDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetWarehousesAsync_KeywordsIsIncluded_ReturnFilteredData()
    {
        var keywords = "keywords";
        var pageIndex = 1; //second page
        var pageSize = 1;
        var warehouse1 = new Warehouse("code-1", "keywords-1", WarehouseType.Main);
        var warehouse2 = new Warehouse("code-2", "keywords-2", WarehouseType.SubWarehouse);
        var warehouse3 = new Warehouse("code-3", "warehouse-3", WarehouseType.ReturnWarehouse);
        var warehouseDataReaderMock = WarehouseDataReader.WithData(warehouse1, warehouse2, warehouse3);
        var warehouseManager = new WarehouseManager(null!, warehouseDataReaderMock.Object, null!);

        var filteredResult = await warehouseManager.GetWarehousesAsync(keywords, pageIndex, pageSize);

        Assert.Equal(2, filteredResult.PagerInfo.TotalCount);
        Assert.Equal(warehouse2.Id, filteredResult.ElementAt(0).Id);
        warehouseDataReaderMock.Verify();
    }

    #endregion
}
