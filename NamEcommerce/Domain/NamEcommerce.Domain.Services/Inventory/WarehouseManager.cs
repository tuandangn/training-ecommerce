using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class WarehouseManager : IWarehouseManager
{
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;

    public WarehouseManager(IRepository<Warehouse> warehouseRepository, IEntityDataReader<Warehouse> warehouseEntityDataReader)
    {
        _warehouseRepository = warehouseRepository;
        _warehouseDataReader = warehouseEntityDataReader;
    }

    public async Task<CreateWarehouseResultDto> CreateWarehouseAsync(CreateWarehouseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (dto.WarehouseType == WarehouseType.DirectTransit && await DirectShipTransitExistsAsync().ConfigureAwait(false))
            throw new DirectShipTransitWarehouseAlreadyExistsException();

        var warehouse = new Warehouse(dto.WarehouseType)
        {
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            ManagerUserId = dto.ManagerUserId,
            DisplayOrder = dto.DisplayOrder
        };
        await warehouse.SetCodeAsync(dto.Code, this).ConfigureAwait(false);
        await warehouse.SetNameAsync(dto.Name, this).ConfigureAwait(false);
        warehouse.SetActive(dto.IsActive);
        warehouse.MarkCreated();

        var insertedWarehouse = await _warehouseRepository.InsertAsync(warehouse).ConfigureAwait(false);

        return new CreateWarehouseResultDto
        {
            CreatedId = insertedWarehouse.Id
        };
    }

    public async Task DeleteWarehouseAsync(Guid id)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(id);

        warehouse.MarkDeleted();

        await _warehouseRepository.DeleteAsync(warehouse).ConfigureAwait(false);
    }

    public Task<bool> DoesCodeExistAsync(string code, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);

        var query = from warehouse in _warehouseDataReader.DataSource
                    where warehouse.Code == code && (comparesWithCurrentId == null || warehouse.Id != comparesWithCurrentId)
                    select warehouse;

        var sameNameExists = query.FirstOrDefault() != null;
        return Task.FromResult(sameNameExists);
    }

    public Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var query = from warehouse in _warehouseDataReader.DataSource
                    where warehouse.Name == name && (comparesWithCurrentId == null || warehouse.Id != comparesWithCurrentId)
                    select warehouse;

        var sameNameExists = query.FirstOrDefault() != null;
        return Task.FromResult(sameNameExists);
    }

    public async Task<WarehouseDto?> GetWarehouseByIdAsync(Guid id)
    {
        var warehouse = await _warehouseDataReader.GetByIdAsync(id, default).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(id);

        return warehouse.ToDto();
    }

    public Task<IPagedDataDto<WarehouseDto>> GetWarehousesAsync(int pageIndex, int pageSize, string? keywords = null, WarehouseType[]? types = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _warehouseDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normizedKeywords = TextHelper.Normalize(keywords);
            query = query.Where(c =>
                c.Name.Value.Contains(keywords)
                || c.Name.Value.Contains(normizedKeywords)
                || c.Name.NormalizedValue.Contains(normizedKeywords)
                || c.Code.Contains(keywords));
        }

        if (types != null && types.Length > 0)
            query = query.Where(c => types.Contains(c.WarehouseType));

        query = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name.Value);

        var totalCount = query.Count();
        var pagedData = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var data = PagedDataDto.Create(pagedData.Select(warehouse => warehouse.ToDto()), pageIndex, pageSize, totalCount);
        return Task.FromResult(data);
    }

    public Task<bool> DirectShipTransitExistsAsync()
    {
        var exists = _warehouseDataReader.DataSource
            .Any(w => w.WarehouseType == WarehouseType.DirectTransit);
        return Task.FromResult(exists);
    }

    public async Task<UpdateWarehouseResultDto> UpdateWarehouseAsync(UpdateWarehouseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var warehouse = await _warehouseRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (warehouse is null)
            throw new ArgumentException("Warehouse is not found", nameof(dto));

        if (dto.WarehouseType == WarehouseType.DirectTransit && warehouse.WarehouseType != WarehouseType.DirectTransit
            && await DirectShipTransitExistsAsync().ConfigureAwait(false))
            throw new DirectShipTransitWarehouseAlreadyExistsException();

        await warehouse.SetCodeAsync(dto.Code, this).ConfigureAwait(false);
        await warehouse.SetNameAsync(dto.Name, this).ConfigureAwait(false);
        warehouse.Address = dto.Address;
        warehouse.PhoneNumber = dto.PhoneNumber;
        warehouse.ManagerUserId = dto.ManagerUserId;
        warehouse.ChangeType(dto.WarehouseType);
        warehouse.DisplayOrder = dto.DisplayOrder;
        warehouse.SetActive(dto.IsActive);
        warehouse.MarkUpdated();

        var result = await _warehouseRepository.UpdateAsync(warehouse).ConfigureAwait(false);

        return new UpdateWarehouseResultDto(result.Id)
        {
            Code = result.Code,
            Name = result.Name,
            IsActive = result.IsActive,
            PhoneNumber = result.PhoneNumber,
            Address = result.Address,
            ManagerUserId = result.ManagerUserId,
            WarehouseType = result.WarehouseType
        };
    }
}
