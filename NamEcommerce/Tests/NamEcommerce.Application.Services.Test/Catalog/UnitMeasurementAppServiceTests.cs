using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Services.Catalog;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Application.Services.Test.Catalog;

public sealed class UnitMeasurementAppServiceTests
{
    #region GetUnitMeasurementsAsync

    [Fact]
    public async Task GetUnitMeasurementsAsync_KeywordIsNotNull_ReturnsOrderedPagedData()
    {
        var keyword = "keyword";
        var pageIndex = 0;
        var pageSize = int.MaxValue;
        var pagedData = PagedDataDto.Create(
            [new UnitMeasurementDto(Guid.NewGuid()) { Name = $"{keyword}-1", DisplayOrder = 1 },
            new UnitMeasurementDto(Guid.NewGuid()) { Name = $"{keyword}-2", DisplayOrder = 2 }],
            pageIndex, pageSize
        );
        var unitMeasurementManagerMock = UnitMeasurementManager.WhenGetUnitMeasurementsReturns(keyword, pageIndex, pageSize, pagedData);
        var unitMeasurementAppService = new UnitMeasurementAppService(unitMeasurementManagerMock.Object, null!);

        var pagedDataResult = await unitMeasurementAppService.GetUnitMeasurementsAsync(keyword, 0, int.MaxValue);

        Assert.Equal(pagedData.ElementAt(0).Id, pagedDataResult.Items.First().Id);
        Assert.Equal(pagedData.ElementAt(1).Id, pagedDataResult.Items.ElementAt(1).Id);
        Assert.Equal(2, pagedDataResult.Pagination.TotalCount);
        unitMeasurementManagerMock.Verify();
    }

    #endregion

    #region GetUnitMeasurementsAsync

    [Fact]
    public async Task GetUnitMeasurementsByIdsAsync_IdsIsNull_ThrowsArgumentNullException()
    {
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, null!);
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => unitMeasurementAppService.GetUnitMeasurementsByIdsAsync(null!));
    }

    [Fact]
    public async Task GetUnitMeasurementsByIdsAsync_IdsIsEmpty_ReturnsEmpty()
    {
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, null!);
        var result = await unitMeasurementAppService.GetUnitMeasurementsByIdsAsync([]);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUnitMeasurementsByIdsAsync_IdsIsNotEmpty_ReturnsFoundUnitMeasurements()
    {
        var unitMeasurement1 = new UnitMeasurement(Guid.NewGuid(), "unitMeasurement-1");
        var unitMeasurement2 = new UnitMeasurement(Guid.NewGuid(), "unitMeasurement-2");
        var unitMeasurement3 = new UnitMeasurement(Guid.NewGuid(), "unitMeasurement-3");
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.SetData(
            unitMeasurement1, unitMeasurement2, unitMeasurement3
        );
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, unitMeasurementDataReaderMock.Object);
        var result = await unitMeasurementAppService.GetUnitMeasurementsByIdsAsync([unitMeasurement1.Id, unitMeasurement2.Id]);

        Assert.NotEmpty(result);
        Assert.Equal(2, result.Count());
        Assert.Equal(unitMeasurement1.Id, result.First().Id);
        Assert.Equal(unitMeasurement2.Id, result.ElementAt(1).Id);
        unitMeasurementDataReaderMock.Verify();
    }

    #endregion

    #region GetUnitMeasurementByIdAsync

    [Fact]
    public async Task GetUnitMeasurementByIdAsync_UnitMeasurementNotFound_ReturnsNull()
    {
        var notFoundId = Guid.NewGuid();
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.NotFound(notFoundId);
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, unitMeasurementDataReaderMock.Object);

        var result = await unitMeasurementAppService.GetUnitMeasurementByIdAsync(notFoundId);

        Assert.Null(result);
        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetUnitMeasurementByIdAsync_UnitMeasurementFound_ReturnsData()
    {
        var findId = Guid.NewGuid();
        var unitMeasurement = new UnitMeasurement(findId, "unitMeasurement");
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.UnitMeasurementById(unitMeasurement.Id, unitMeasurement);
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, unitMeasurementDataReaderMock.Object);

        var result = await unitMeasurementAppService.GetUnitMeasurementByIdAsync(findId);

        Assert.Equal(unitMeasurement.Id, result!.Id);
        unitMeasurementDataReaderMock.Verify();
    }

    #endregion

    #region CreateUnitMeasurementAsync

    [Fact]
    public async Task CreateUnitMeasurementAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => unitMeasurementAppService.CreateUnitMeasurementAsync(null!));
    }

    [Fact]
    public async Task CreateUnitMeasurementAsync_DtoIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new CreateUnitMeasurementAppDto
        {
            Name = string.Empty,
            DisplayOrder = 0
        };
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, null!);
        var falseResult = await unitMeasurementAppService.CreateUnitMeasurementAsync(invalidDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
    }

    [Fact]
    public async Task CreateUnitMeasurementAsync_NameIsExists_ThrowsUnitMeasurementNameExistsException()
    {
        var existingName = "existing-name";
        var existingNameDto = new CreateUnitMeasurementAppDto
        {
            Name = existingName,
            DisplayOrder = 0
        };
        var unitMeasurementManager = UnitMeasurementManager.SetUsernameExists(existingName, true);
        var unitMeasurementAppService = new UnitMeasurementAppService(unitMeasurementManager.Object, null!);

        var falseResult = await unitMeasurementAppService.CreateUnitMeasurementAsync(existingNameDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        unitMeasurementManager.Verify();
    }

    [Fact]
    public async Task CreateUnitMeasurementAsync_NameIsNotExists_ReturnsResult()
    {
        var dto = new CreateUnitMeasurementAppDto
        {
            Name = "new-unit-measurement",
            DisplayOrder = 0
        };
        var createUnitMeasurementResult = new Domain.Shared.Dtos.Catalog.CreateUnitMeasurementResultDto
        {
            CreatedId = Guid.NewGuid()
        };
        var unitMeasurementManager = UnitMeasurementManager.SetUsernameExists(dto.Name, false)
            .CreateUnitMeasurementReturns(new Domain.Shared.Dtos.Catalog.CreateUnitMeasurementDto
            {
                Name = dto.Name,
                DisplayOrder = dto.DisplayOrder
            }, createUnitMeasurementResult);
        var unitMeasurementAppService = new UnitMeasurementAppService(unitMeasurementManager.Object, null!);

        var result = await unitMeasurementAppService.CreateUnitMeasurementAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(createUnitMeasurementResult.CreatedId, result.CreatedId);
        unitMeasurementManager.Verify();
    }

    #endregion

    #region DeleteUnitMeasurementAsync

    [Fact]
    public async Task DeleteUnitMeasurementAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => unitMeasurementAppService.DeleteUnitMeasurementAsync(null!));
    }

    [Fact]
    public async Task DeleteUnitMeasurementAsync_UnitMeasurementNotFound_ReturnsFalseResult()
    {
        var notFoundId = Guid.NewGuid();
        var notFoundDto = new DeleteUnitMeasurementAppDto(notFoundId);
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.NotFound(notFoundId);
        var unitMeasurementAppService = new UnitMeasurementAppService(null!, unitMeasurementDataReaderMock.Object);

        var falseResult = await unitMeasurementAppService.DeleteUnitMeasurementAsync(notFoundDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteUnitMeasurementAsync_UnitMeasurementFound_DeleteAndReturns()
    {
        var dto = new DeleteUnitMeasurementAppDto(Guid.NewGuid());
        var unitMeasurement = new UnitMeasurement(dto.Id, "unit-measurement");
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.UnitMeasurementById(dto.Id, unitMeasurement);
        var unitMeasurementManagerMock = UnitMeasurementManager.CanDeleteUnitMeasurement(dto.Id);
        var unitMeasurementAppService = new UnitMeasurementAppService(unitMeasurementManagerMock.Object, unitMeasurementDataReaderMock.Object);

        var result = await unitMeasurementAppService.DeleteUnitMeasurementAsync(dto);

        Assert.True(result.Success);
        unitMeasurementDataReaderMock.Verify();
        unitMeasurementManagerMock.Verify();
    }

    #endregion
}
