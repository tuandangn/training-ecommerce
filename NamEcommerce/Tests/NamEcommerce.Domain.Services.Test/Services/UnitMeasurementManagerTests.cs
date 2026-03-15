using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Services.Test.Helpers;
using System.Linq.Expressions;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class UnitMeasurementManagerTests
{

    #region CreateUnitMeasurementAsync

    [Fact]
    public async Task CreateUnitMeasurementAsync_InputDtoIsNull_ThrowsArgumentNullException()
    {
        var unitMeasurementManager = new UnitMeasurementManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            unitMeasurementManager.CreateUnitMeasurementAsync(null!)
        );
    }

    [Fact]
    public async Task CreateUnitMeasurementAsync_DtoIsValid_ReturnsCreatedUnitMeasurementDto()
    {
        var unitMeasurement = new UnitMeasurement(Guid.NewGuid(), "name") { DisplayOrder = 1 };
        var returnUnitMeasurement = new UnitMeasurement(unitMeasurement.Id, unitMeasurement.Name)
        {
            DisplayOrder = unitMeasurement.DisplayOrder
        };
        var unitMeasurementRepositoryMock = UnitMeasurementRepository.CreateUnitMeasurementWillReturns(unitMeasurement, returnUnitMeasurement);
        var unitMeasurementDataReaderStub = UnitMeasurementDataReader.Empty();
        var unitMeasurementManager = new UnitMeasurementManager(unitMeasurementRepositoryMock.Object, unitMeasurementDataReaderStub.Object);

        var unitMeasurementDto = await unitMeasurementManager.CreateUnitMeasurementAsync(
            new CreateUnitMeasurementDto(unitMeasurement.Name) { DisplayOrder = unitMeasurement.DisplayOrder });

        Assert.Equal(unitMeasurementDto, returnUnitMeasurement.ToDto());
        unitMeasurementRepositoryMock.Verify();
    }

    #endregion


    #region DeleteUnitMeasurementAsync

    [Fact]
    public async Task DeleteUnitMeasurementAsync_UnitMeasurementIsNotFound_ThrowsArgumentException()
    {
        var notFoundUnitMeasurementId = Guid.NewGuid();
        var unitMeasurementRepositoryMock = UnitMeasurementRepository.NotFound(notFoundUnitMeasurementId);
        var unitMeasurementManager = new UnitMeasurementManager(unitMeasurementRepositoryMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => unitMeasurementManager.DeleteUnitMeasurementAsync(notFoundUnitMeasurementId));

        unitMeasurementRepositoryMock.Verify();
    }

    [Fact]
    public async Task DeleteUnitMeasurementAsync_DeleteUnitMeasurement()
    {
        var unitMeasurement = new UnitMeasurement(Guid.NewGuid(), string.Empty);
        var unitMeasurementRepositoryMock = UnitMeasurementRepository.UnitMeasurementById(unitMeasurement.Id, unitMeasurement);
        var unitMeasurementManager = new UnitMeasurementManager(unitMeasurementRepositoryMock.Object, null!);

        await unitMeasurementManager.DeleteUnitMeasurementAsync(unitMeasurement.Id);

        unitMeasurementRepositoryMock.Verify();
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
        var unitMeasurementRepositoryMock = UnitMeasurementRepository.NotFound(notFoundUnitMeasurementId);
        var unitMeasurementManager = new UnitMeasurementManager(unitMeasurementRepositoryMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => unitMeasurementManager.UpdateUnitMeasurementAsync(new UpdateUnitMeasurementDto(notFoundUnitMeasurementId, string.Empty)));
        unitMeasurementRepositoryMock.Verify();
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
        var unitMeasurementRepositoryMock = UnitMeasurementRepository.UnitMeasurementById(oldUnitMeasurement.Id, oldUnitMeasurement)
            .WhenCall(repository => repository.UpdateAsync(It.Is(isUnitMeasurementMatch), default), updateUnitMeasurement);
        var unitMeasurementDataReaderStub = UnitMeasurementDataReader.Empty();
        var unitMeasurementManager = new UnitMeasurementManager(unitMeasurementRepositoryMock.Object, unitMeasurementDataReaderStub.Object);

        var resultUnitMeasurement = await unitMeasurementManager.UpdateUnitMeasurementAsync(
            new UpdateUnitMeasurementDto(updateUnitMeasurement.Id, updateUnitMeasurement.Name)
            {
                DisplayOrder = updateUnitMeasurement.DisplayOrder,
            });

        Assert.Equal(resultUnitMeasurement, updateUnitMeasurement.ToDto());
        unitMeasurementRepositoryMock.Verify();
    }

    #endregion
}
