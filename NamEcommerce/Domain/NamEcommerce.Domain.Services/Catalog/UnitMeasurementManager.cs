using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Catalog;

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

    public async Task<CreateUnitMeasurementResultDto> CreateUnitMeasurementAsync(CreateUnitMeasurementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesNameExistAsync(dto.Name, null).ConfigureAwait(false))
            throw new UnitMeasurementNameExistsException(dto.Name);

        var unitMeasurement = new UnitMeasurement(Guid.NewGuid(), dto.Name)
        {
            DisplayOrder = dto.DisplayOrder
        };
        unitMeasurement.SetDecimalPlaces(dto.DecimalPlaces);
        unitMeasurement.MarkCreated();

        var insertedUnitMeasurement = await _unitMeasurementRepository.InsertAsync(unitMeasurement).ConfigureAwait(false);

        return new CreateUnitMeasurementResultDto
        {
            CreatedId = insertedUnitMeasurement.Id
        };
    }

    public async Task DeleteUnitMeasurementAsync(Guid id)
    {
        var unitMeasurement = await _unitMeasurementRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (unitMeasurement is null)
            throw new ArgumentException("Unit measurement is not found", nameof(id));

        unitMeasurement.MarkDeleted();

        await _unitMeasurementRepository.DeleteAsync(unitMeasurement).ConfigureAwait(false);
    }

    public async Task<UpdateUnitMeasurementResultDto> UpdateUnitMeasurementAsync(UpdateUnitMeasurementDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var unitMeasurement = await _unitMeasurementRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (unitMeasurement is null)
            throw new ArgumentException("Unit measurement is not found", nameof(dto));

        await unitMeasurement.SetNameAsync(dto.Name, this).ConfigureAwait(false);
        unitMeasurement.DisplayOrder = dto.DisplayOrder;
        unitMeasurement.SetDecimalPlaces(dto.DecimalPlaces);
        unitMeasurement.MarkUpdated();

        var result = await _unitMeasurementRepository.UpdateAsync(unitMeasurement).ConfigureAwait(false);

        return new UpdateUnitMeasurementResultDto(result.Id)
        {
            Name = result.Name,
            DisplayOrder = result.DisplayOrder,
            DecimalPlaces = result.DecimalPlaces
        };
    }

    public async Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var query = from unitMesuarement in _unitMeasurementDataReader.DataSource
                    where unitMesuarement.Name == name && (comparesWithCurrentId == null || unitMesuarement.Id != comparesWithCurrentId)
                    select unitMesuarement;

        var sameNameExists = await query.FirstOrDefaultAsync().ConfigureAwait(false) != null;
        return sameNameExists;
    }

    public async Task<IPagedDataDto<UnitMeasurementDto>> GetUnitMeasurementsAsync(string? keywords, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _unitMeasurementDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normizedKeywords = TextHelper.Normalize(keywords);
            query = query.Where(c => c.Name.Contains(keywords) || c.Name.Contains(normizedKeywords) || c.NormalizedName.Contains(normizedKeywords));
        }

        query = query.OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name);

        var totalCount = await query.CountAsync().ConfigureAwait(false);
        var pagedData = await query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToListAsync().ConfigureAwait(false);

        var data = PagedDataDto.Create(pagedData.Select(unitMeasurement => unitMeasurement.ToDto()), pageIndex, pageSize, totalCount);
        return data;
    }
}
