using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Services.Test.Helpers;
using System.Linq.Expressions;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class UnitMeasurementManagerTests
{
    #region CreateUnitMeasurementAsync

    [Fact]
    public async Task CreateUnitMeasurementAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var unitMeasurementManager = new UnitMeasurementManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            unitMeasurementManager.CreateUnitMeasurementAsync(null!)
        );
    }

    [Fact]
    public async Task CreateUnitMeasurementAsync_NameIsExists_ThrowsUnitMeasurementNameExistsException()
    {
        var testName = "existing-name";
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.HasOne(new UnitMeasurement(default, testName));
        var unitMeasurementManager = new UnitMeasurementManager(null!, unitMeasurementDataReaderMock.Object);

        await Assert.ThrowsAsync<UnitMeasurementNameExistsException>(() =>
            unitMeasurementManager.CreateUnitMeasurementAsync(new CreateUnitMeasurementDto { Name = testName })
        );
        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateUnitMeasurementAsync_DataIsValid_ReturnsCreatedUnitMeasurement()
    {
        var unitMeasurement = new UnitMeasurement(Guid.NewGuid(), "name") { DisplayOrder = 1 };
        var returnUnitMeasurement = new UnitMeasurement(unitMeasurement.Id, unitMeasurement.Name)
        {
            DisplayOrder = unitMeasurement.DisplayOrder
        };
        var unitMeasurementRepositoryMock = UnitMeasurementRepository.CreateUnitMeasurementWillReturns(returnUnitMeasurement);
        var unitMeasurementDataReaderStub = UnitMeasurementDataReader.Empty();
        var unitMeasurementManager = new UnitMeasurementManager(unitMeasurementRepositoryMock.Object, unitMeasurementDataReaderStub.Object);

        var resultDto = await unitMeasurementManager.CreateUnitMeasurementAsync(
            new CreateUnitMeasurementDto
            {
                Name = unitMeasurement.Name,
                DisplayOrder = unitMeasurement.DisplayOrder
            });

        Assert.Equal(unitMeasurement.Id, resultDto.CreatedId);
        unitMeasurementRepositoryMock.Verify();
    }

    #endregion

    #region DeleteUnitMeasurementAsync

    [Fact]
    public async Task DeleteUnitMeasurementAsync_UnitMeasurementIsNotFound_ThrowsArgumentException()
    {
        var notFoundUnitMeasurementId = Guid.NewGuid();
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.NotFound(notFoundUnitMeasurementId);
        var unitMeasurementManager = new UnitMeasurementManager(null!, unitMeasurementDataReaderMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(()
            => unitMeasurementManager.DeleteUnitMeasurementAsync(notFoundUnitMeasurementId));

        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteUnitMeasurementAsync_DeleteUnitMeasurement()
    {
        var unitMeasurement = new UnitMeasurement(Guid.NewGuid(), "unit-measurement");
        var unitMeasurementDataRepositoryMock = UnitMeasurementRepository.CanDeleteUnitMeasurement(unitMeasurement);
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.UnitMeasurementById(unitMeasurement.Id, unitMeasurement);
        var unitMeasurementManager = new UnitMeasurementManager(unitMeasurementDataRepositoryMock.Object, unitMeasurementDataReaderMock.Object);

        await unitMeasurementManager.DeleteUnitMeasurementAsync(unitMeasurement.Id);

        unitMeasurementDataReaderMock.Verify();
    }

    #endregion

    #region UpdateUnitMeasurementAsync

    [Fact]
    public async Task UpdateUnitMeasurementAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var unitMeasurementManager = new UnitMeasurementManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => unitMeasurementManager.UpdateUnitMeasurementAsync(null!));
    }

    [Fact]
    public async Task UpdateUnitMeasurementAsync_UnitMeasurementIsNotFound_ThrowsArgumentException()
    {
        var notFoundUnitMeasurementId = Guid.NewGuid();
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.NotFound(notFoundUnitMeasurementId);
        var unitMeasurementManager = new UnitMeasurementManager(null!, unitMeasurementDataReaderMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(()
            => unitMeasurementManager.UpdateUnitMeasurementAsync(new UpdateUnitMeasurementDto(notFoundUnitMeasurementId)
            {
                Name = string.Empty
            }));
        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateUnitMeasurementAsync_UnitMeasurementNameIsExists_ThrowsUnitMeasurementNameExistsException()
    {
        var oldUnitMeasurement = new UnitMeasurement(Guid.NewGuid(), "parent-old");
        var updateUnitMeasurement = oldUnitMeasurement with
        {
            Name = "parent-new"
        };
        var sameNameCategoryId = Guid.NewGuid();
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader
            .HasOne(new UnitMeasurement(default, updateUnitMeasurement.Name))
            .UnitMeasurementById(oldUnitMeasurement.Id, oldUnitMeasurement);
        var unitMeasurementManager = new UnitMeasurementManager(null!, unitMeasurementDataReaderMock.Object);

        await Assert.ThrowsAsync<UnitMeasurementNameExistsException>(()
            => unitMeasurementManager.UpdateUnitMeasurementAsync(new UpdateUnitMeasurementDto(updateUnitMeasurement.Id)
            {
                Name = updateUnitMeasurement.Name
            }));

        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateUnitMeasurementAsync_UpdateUnitMeasurement()
    {
        var oldUnitMeasurement = new UnitMeasurement(Guid.NewGuid(), "parent-old")
        {
            DisplayOrder = 1
        };
        var updateUnitMeasurement = oldUnitMeasurement with
        {
            Name = "parent-new",
            DisplayOrder = 2
        };
        Expression<Func<UnitMeasurement, bool>> isUnitMeasurementMatch =
            c => c.Id == updateUnitMeasurement.Id
                && c.Name == updateUnitMeasurement.Name
                && c.DisplayOrder == updateUnitMeasurement.DisplayOrder;
        var unitMeasurementRepositoryMock = Repository.Create<UnitMeasurement>()
            .WhenCall(repository => repository.UpdateAsync(It.Is(isUnitMeasurementMatch), default), updateUnitMeasurement);
        var unitMeasurementDataReaderStub = UnitMeasurementDataReader.UnitMeasurementById(oldUnitMeasurement.Id, oldUnitMeasurement);
        var unitMeasurementManager = new UnitMeasurementManager(unitMeasurementRepositoryMock.Object, unitMeasurementDataReaderStub.Object);

        var resultUnitMeasurement = await unitMeasurementManager.UpdateUnitMeasurementAsync(
            new UpdateUnitMeasurementDto(updateUnitMeasurement.Id)
            {
                Name = updateUnitMeasurement.Name,
                DisplayOrder = updateUnitMeasurement.DisplayOrder,
            });

        Assert.Equal(resultUnitMeasurement, resultUnitMeasurement with
        {
            Id = updateUnitMeasurement.Id,
            Name = updateUnitMeasurement.Name,
            DisplayOrder = updateUnitMeasurement.DisplayOrder
        });
        unitMeasurementRepositoryMock.Verify();
    }

    #endregion

    #region DoesNameExistAsync

    [Fact]
    public async Task DoesNameExistAsync_NameIsNull_ThrowsArgumentException()
    {
        var unitMeasurmentManager = new UnitMeasurementManager(null!, null!);

        await Assert.ThrowsAnyAsync<ArgumentException>(() => unitMeasurmentManager.DoesNameExistAsync(null!));
    }

    [Fact]
    public async Task DoesNameExistAsync_NameDoesExist_ReturnsTrue()
    {
        var existingName = "existing-name";
        var existingUnitMeasurement = new UnitMeasurement(Guid.NewGuid(), existingName);
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.HasOne(existingUnitMeasurement);
        var unitMeasurmentManager = new UnitMeasurementManager(null!, unitMeasurementDataReaderMock.Object);

        var exists = await unitMeasurmentManager.DoesNameExistAsync(existingName);

        Assert.True(exists);
        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesNameExistAsync_NameDoesExistButItself_ReturnsFalse()
    {
        var unitMeasurement = new UnitMeasurement(Guid.NewGuid(), "unit-measurement");
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.HasOne(unitMeasurement);
        var unitMeasurmentManager = new UnitMeasurementManager(null!, unitMeasurementDataReaderMock.Object);

        var @false = await unitMeasurmentManager.DoesNameExistAsync(unitMeasurement.Name, unitMeasurement.Id);

        Assert.False(@false);
        unitMeasurementDataReaderMock.Verify();
    }

    #endregion

    #region GetUnitMeasurementsAsync

    [Fact]
    public async Task GetUnitMeasurementsAsync_PageIndexLessThanZero_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageIndex = -1;
        var unitMeasurementManager = new UnitMeasurementManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            unitMeasurementManager.GetUnitMeasurementsAsync("keywords", invalidPageIndex, int.MaxValue));
    }

    [Fact]
    public async Task GetUnitMeasurementsAsync_PageSizeLessThanOrEqualZero_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageSize = 0;
        var unitMeasurementManager = new UnitMeasurementManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            unitMeasurementManager.GetUnitMeasurementsAsync("keywords", 0, invalidPageSize));
    }

    [Fact]
    public async Task GetUnitMeasurementsAsync_KeywordsIsEmpty_ReturnPagedOrderedData()
    {
        var pageIndex = 0;
        var pageSize = 2;
        var unitMeasurement1 = new UnitMeasurement(Guid.NewGuid(), "unit-measurement-1")
        {
            DisplayOrder = 2
        };
        var unitMeasurement2 = new UnitMeasurement(Guid.NewGuid(), "unit-measurement-2")
        {
            DisplayOrder = 1 //first
        };
        var unitMeasurement3 = new UnitMeasurement(Guid.NewGuid(), "unit-measurement-3")
        {
            DisplayOrder = 1 //second
        };
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.WithData(unitMeasurement1, unitMeasurement2, unitMeasurement3);
        var unitMeasurementManager = new UnitMeasurementManager(null!, unitMeasurementDataReaderMock.Object);

        var pagedOrderedResult = await unitMeasurementManager.GetUnitMeasurementsAsync("", pageIndex, pageSize);

        Assert.Equal(3, pagedOrderedResult.PagerInfo.TotalCount);
        Assert.Equal(unitMeasurement2.Id, pagedOrderedResult.ElementAt(0).Id);
        Assert.Equal(unitMeasurement3.Id, pagedOrderedResult.ElementAt(1).Id);
        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetUnitMeasurementsAsync_KeywordsIsIncluded_ReturnFilteredData()
    {
        var keywords = "keywords";
        var pageIndex = 1; //second page
        var pageSize = 1;
        var unitMeasurement1 = new UnitMeasurement(Guid.NewGuid(), "keywords-1");
        var unitMeasurement2 = new UnitMeasurement(Guid.NewGuid(), "keywords-2");
        var unitMeasurement3 = new UnitMeasurement(Guid.NewGuid(), "unit-measurement");
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.WithData(unitMeasurement1, unitMeasurement2, unitMeasurement3);
        var unitMeasurementManager = new UnitMeasurementManager(null!, unitMeasurementDataReaderMock.Object);

        var filteredResult = await unitMeasurementManager.GetUnitMeasurementsAsync(keywords, pageIndex, pageSize);

        Assert.Equal(2, filteredResult.PagerInfo.TotalCount);
        Assert.Equal(unitMeasurement2.Id, filteredResult.ElementAt(0).Id);
        unitMeasurementDataReaderMock.Verify();
    }

    #endregion
}
