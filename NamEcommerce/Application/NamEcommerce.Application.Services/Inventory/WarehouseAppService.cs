using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Inventory;

public sealed class WarehouseAppService : IWarehouseAppService
{
    private readonly IWarehouseManager _warehouseManager;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;

    public WarehouseAppService(IWarehouseManager warehouseManager, IEntityDataReader<Warehouse> warehouseDataReader)
    {
        _warehouseManager = warehouseManager;
        _warehouseDataReader = warehouseDataReader;
    }

    public async Task<CreateWarehouseResultAppDto> CreateWarehouseAsync(CreateWarehouseAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreateWarehouseResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (await _warehouseManager.DoesNameExistAsync(dto.Name).ConfigureAwait(false))
        {
            return new CreateWarehouseResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.WarehouseNameAlreadyExists"
            };
        }

        var result = await _warehouseManager.CreateWarehouseAsync(new CreateWarehouseDto
        {
            Code = dto.Code,
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            ManagerUserId = dto.ManagerUserId,
            WarehouseType = (WarehouseType) dto.WarehouseType,
            IsActive = dto.IsActive
        }).ConfigureAwait(false);

        return new CreateWarehouseResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<DeleteWarehouseResultAppDto> DeleteWarehouseAsync(DeleteWarehouseAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var warehouse = await _warehouseDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (warehouse == null)
        {
            return new DeleteWarehouseResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.WarehouseIsNotFound"
            };
        }

        await _warehouseManager.DeleteWarehouseAsync(dto.Id).ConfigureAwait(false);

        return new DeleteWarehouseResultAppDto { Success = true };
    }

    public CommonOptionListDto GetAvailableWarehouseTypes()
    {
        var availableWarehouseTypes = Enum.GetValues<WarehouseType>()
             .Select(warehouseType => new CommonOptionListDto.OptionItemDto
             {
                 Text = $"Enums.{typeof(WarehouseType).FullName}.{warehouseType}",
                 Value = ((int)warehouseType).ToString()
             });

        return new CommonOptionListDto { Items = availableWarehouseTypes };
    }

    public async Task<WarehouseAppDto?> GetWarehouseByIdAsync(Guid id)
    {
        var warehouse = await _warehouseDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return warehouse?.ToDto();
    }

    public async Task<IPagedDataAppDto<WarehouseAppDto>> GetWarehousesAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var pagedData = await _warehouseManager.GetWarehousesAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);
        var result = PagedDataAppDto.Create(
            pagedData.Select(warehouse => warehouse.ToDto()),
            pageIndex, pageSize, pagedData.PagerInfo.TotalCount);

        return result;
    }

    public async Task<UpdateWarehouseResultAppDto> UpdateWarehouseAsync(UpdateWarehouseAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdateWarehouseResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var warehouse = await _warehouseDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (warehouse == null)
        {
            return new UpdateWarehouseResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.WarehouseIsNotFound"
            };
        }

        if (await _warehouseManager.DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
        {
            return new UpdateWarehouseResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.WarehouseNameAlreadyExists"
            };
        }

        var result = await _warehouseManager.UpdateWarehouseAsync(new UpdateWarehouseDto(dto.Id)
        {
            Code = dto.Code,
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            ManagerUserId = dto.ManagerUserId,
            WarehouseType = (WarehouseType) dto.WarehouseType,
            IsActive = dto.IsActive
        }).ConfigureAwait(false);

        return new UpdateWarehouseResultAppDto
        {
            Success = true,
            UpdatedId = result.Id
        };
    }
}
