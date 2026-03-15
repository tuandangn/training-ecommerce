using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Catalog;

public sealed class UnitMeasurementManager : IUnitMeasurementManager
{
    private readonly IRepository<UnitMeasurement> _unitMeasurementRepository;
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;

    public UnitMeasurementManager(IRepository<UnitMeasurement> unitMeasurementRepository, IEntityDataReader<UnitMeasurement> unitMeasurementDataReader)
    {
        _unitMeasurementRepository = unitMeasurementRepository;
        _unitMeasurementDataReader = unitMeasurementDataReader;
    }

    public async Task<UnitMeasurementDto> CreateUnitMeasurementAsync(CreateUnitMeasurementDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var insertedUnitMeasurement = await _unitMeasurementRepository.InsertAsync(
            new UnitMeasurement(Guid.NewGuid(), dto.Name)
            {
                DisplayOrder = dto.DisplayOrder
            }).ConfigureAwait(false);
        return insertedUnitMeasurement.ToDto();
    }

    public async Task DeleteUnitMeasurementAsync(Guid id)
    {
        var unitMeasurement = await _unitMeasurementRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (unitMeasurement is null)
            throw new ArgumentException("Unit measurement is not found", nameof(id));

        await _unitMeasurementRepository.DeleteAsync(unitMeasurement).ConfigureAwait(false);
    }

    public async Task<UnitMeasurementDto> UpdateUnitMeasurementAsync(UpdateUnitMeasurementDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var unitMeasurement = await _unitMeasurementRepository.GetByIdAsync(dto.Id);
        if (unitMeasurement is null)
            throw new ArgumentException("Unit measurement is not found", nameof(dto));

        var result = await _unitMeasurementRepository.UpdateAsync(unitMeasurement with
        {
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder
        }).ConfigureAwait(false);

        return result.ToDto();
    }
}
