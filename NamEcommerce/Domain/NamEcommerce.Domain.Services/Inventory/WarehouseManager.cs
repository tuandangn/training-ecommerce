using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class WarehouseManager : IWarehouseManager
{
    private readonly IRepository<Warehouse> _warehouseRepository;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;
    private readonly IEventPublisher _eventPublisher;

    public WarehouseManager(IRepository<Warehouse> warehouseRepository, IEntityDataReader<Warehouse> warehouseEntityDataReader, IEventPublisher eventPublisher)
    {
        _warehouseRepository = warehouseRepository;
        _warehouseDataReader = warehouseEntityDataReader;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreateWarehouseResultDto> CreateWarehouseAsync(CreateWarehouseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesCodeExistAsync(dto.Code).ConfigureAwait(false))
            throw new WarehouseCodeExistsException(dto.Code);
        if (await DoesNameExistAsync(dto.Name).ConfigureAwait(false))
            throw new WarehouseNameExistsException(dto.Name);

        var warehouse = new Warehouse(dto.Code, dto.Name, (WarehouseType)dto.WarehouseType)
        {
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            ManagerUserId = dto.ManagerUserId
        };
        warehouse.SetActive(dto.IsActive);
        var insertedWarehouse = await _warehouseRepository.InsertAsync(warehouse).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(insertedWarehouse).ConfigureAwait(false);

        return new CreateWarehouseResultDto
        {
            CreatedId = insertedWarehouse.Id
        };
    }

    public async Task DeleteWarehouseAsync(Guid id)
    {
        var warehouse = await _warehouseDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(id);

        await _warehouseRepository.DeleteAsync(warehouse).ConfigureAwait(false);

        await _eventPublisher.EntityDeleted(warehouse).ConfigureAwait(false);
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
        var warehouse = await _warehouseDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(id);

        return warehouse.ToDto();
    }

    public Task<IPagedDataDto<WarehouseDto>> GetWarehousesAsync(string? keywords, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _warehouseDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normizedKeywords = TextHelper.Normalize(keywords);
            query = query.Where(c => c.NormalizedName.Contains(normizedKeywords) || c.Code.Contains(keywords));
        }

        query = query.OrderBy(c => c.Name);

        var totalCount = query.Count();
        var pagedData = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var data = PagedDataDto.Create(pagedData.Select(warehouse => warehouse.ToDto()), pageIndex, pageSize, totalCount);
        return Task.FromResult(data);
    }

    public async Task<UpdateWarehouseResultDto> UpdateWarehouseAsync(UpdateWarehouseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var warehouse = await _warehouseDataReader.GetByIdAsync(dto.Id);
        if (warehouse is null)
            throw new ArgumentException("Warehouse is not found", nameof(dto));

        if(await DoesCodeExistAsync(dto.Code, dto.Id).ConfigureAwait(false))
            throw new WarehouseCodeExistsException(dto.Code);
        warehouse.Code = dto.Code;
        await warehouse.SetNameAsync(dto.Name, this).ConfigureAwait(false);
        warehouse.Address = dto.Address;
        warehouse.PhoneNumber = dto.PhoneNumber;
        warehouse.ManagerUserId = dto.ManagerUserId;
        warehouse.ChangeType((WarehouseType)dto.WarehouseType);
        warehouse.SetActive(dto.IsActive);

        var result = await _warehouseRepository.UpdateAsync(warehouse).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(warehouse).ConfigureAwait(false);

        return new UpdateWarehouseResultDto(result.Id)
        {
            Code = result.Code,
            Name = result.Name,
            IsActive = result.IsActive,
            PhoneNumber = result.PhoneNumber,
            Address = result.Address,
            ManagerUserId = result.ManagerUserId,
            WarehouseType = (int)result.WarehouseType
        };
    }
}
