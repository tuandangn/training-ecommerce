using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Application.Services.Catalog;

public sealed class UnitMeasurementAppService : IUnitMeasurementAppService
{
    private readonly IUnitMeasurementManager _unitMeasurementManager;
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;

    public UnitMeasurementAppService(IUnitMeasurementManager unitMeasurementManager, IEntityDataReader<UnitMeasurement> unitMeasurementDataReader)
    {
        _unitMeasurementManager = unitMeasurementManager;
        _unitMeasurementDataReader = unitMeasurementDataReader;
    }

    public async Task<IPagedDataAppDto<UnitMeasurementAppDto>> GetUnitMeasurementsAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var pagedData = await _unitMeasurementManager.GetUnitMeasurementsAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);
        var result = PagedDataAppDto.Create(
            pagedData.Select(unitMeasurement => unitMeasurement.ToDto()),
            pageIndex, pageSize, pagedData.PagerInfo.TotalCount);

        return result;
    }

    public async Task<IEnumerable<UnitMeasurementAppDto>> GetUnitMeasurementsByIdsAsync(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (!ids.Any())
            return Enumerable.Empty<UnitMeasurementAppDto>();

        var query = from unitMeasurement in _unitMeasurementDataReader.DataSource
                    where ids.Contains(unitMeasurement.Id)
                    select unitMeasurement;
        var unitMeasurements = query.ToList();

        return unitMeasurements.Select(unitMeasurement => unitMeasurement.ToDto());
    }

    public async Task<UnitMeasurementAppDto?> GetUnitMeasurementByIdAsync(Guid id)
    {
        var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return unitMeasurement?.ToDto();
    }

    public async Task<CreateUnitMeasurementResultAppDto> CreateUnitMeasurementAsync(CreateUnitMeasurementAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreateUnitMeasurementResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (await _unitMeasurementManager.DoesNameExistAsync(dto.Name).ConfigureAwait(false))
        {
            return new CreateUnitMeasurementResultAppDto
            {
                Success = false,
                ErrorMessage = "Name already exists."
            };
        }

        var result = await _unitMeasurementManager.CreateUnitMeasurementAsync(new CreateUnitMeasurementDto
        {
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder
        }).ConfigureAwait(false);

        return new CreateUnitMeasurementResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<UpdateUnitMeasurementResultAppDto> UpdateUnitMeasurementAsync(UpdateUnitMeasurementAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdateUnitMeasurementResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (unitMeasurement == null)
        {
            return new UpdateUnitMeasurementResultAppDto
            {
                Success = false,
                ErrorMessage = "Không tìm thấy đơn vị tính"
            };
        }

        if (await _unitMeasurementManager.DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
        {
            return new UpdateUnitMeasurementResultAppDto
            {
                Success = false,
                ErrorMessage = "Tên đơn vị tính trùng lặp"
            };
        }

        var result = await _unitMeasurementManager.UpdateUnitMeasurementAsync(new UpdateUnitMeasurementDto(dto.Id)
        {
            Name = dto.Name,
            DisplayOrder = dto.DisplayOrder
        }).ConfigureAwait(false);

        return new UpdateUnitMeasurementResultAppDto
        {
            Success = true,
            UpdatedId = result.Id
        };
    }

    public async Task<DeleteUnitMeasurementResultAppDto> DeleteUnitMeasurementAsync(DeleteUnitMeasurementAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (unitMeasurement == null)
        {
            return new DeleteUnitMeasurementResultAppDto
            {
                Success = false,
                ErrorMessage = "Unit measurement is not found."
            };
        }

        await _unitMeasurementManager.DeleteUnitMeasurementAsync(dto.Id).ConfigureAwait(false);

        return new DeleteUnitMeasurementResultAppDto { Success = true };
    }
}
