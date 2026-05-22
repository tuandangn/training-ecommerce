using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Inventory;

public sealed class InventoryAppService : IInventoryAppService
{
    private readonly IInventoryStockManager _stockManager;
    private readonly IEntityDataReader<ProductReservationLedger> _reservationLedgerReader;
    private readonly IEntityDataReader<Product> _productReader;
    private readonly IEntityDataReader<Order> _orderReader;

    public InventoryAppService(
        IInventoryStockManager stockManager,
        IEntityDataReader<ProductReservationLedger> reservationLedgerReader,
        IEntityDataReader<Product> productReader,
        IEntityDataReader<Order> orderReader)
    {
        _stockManager = stockManager;
        _reservationLedgerReader = reservationLedgerReader;
        _productReader = productReader;
        _orderReader = orderReader;
    }

    public async Task<IPagedDataAppDto<InventoryStockAppDto>> GetInventoryStocksAsync(string? keywords, Guid? warehouseId, int pageIndex, int pageSize)
    {
        var (total, dataItems) = await _stockManager.GetInventoryStocksAsync(keywords, warehouseId, pageIndex, pageSize);
        var productIds = dataItems.Select(x => x.ProductId).Distinct().ToList();
        var reservedByOrder = _reservationLedgerReader.DataSource
            .Where(x => productIds.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityDelta));

        var items = dataItems.Select(x => new InventoryStockAppDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.WarehouseName,
            QuantityOnHand = x.QuantityOnHand,
            QuantityReserved = x.QuantityReserved,
            TotalReservedByOrder = reservedByOrder.GetValueOrDefault(x.ProductId),
            QuantityAvailable = x.QuantityAvailable,
            UpdatedOnUtc = x.UpdatedOnUtc,
            ReorderLevel = x.ReorderLevel,
            MaxStockLevel = x.MaxStockLevel
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, total);
    }

    public async Task<IEnumerable<ProductInventoryStockInfoAppDto>> GetInventoryStocksForProductAsync(Guid productId, Guid? warehouseId)
    {
        var stockItems = await _stockManager.GetInventoryStocksForProductAsync(productId, warehouseId);

        var productStockInfoDtos = stockItems.Select(stockItem => new ProductInventoryStockInfoAppDto
        {
            ProductId = productId,
            ProductName = stockItem.ProductName,
            WarehouseId = stockItem.WarehouseId,
            WarehouseName = stockItem.WarehouseName,
            QuantityOnHand = stockItem.QuantityOnHand,
            QuantityReserved = stockItem.QuantityReserved,
            QuantityAvailable = stockItem.QuantityAvailable,
            UpdatedOnUtc = stockItem.UpdatedOnUtc
        });

        return productStockInfoDtos;
    }

    public Task<decimal> GetGlobalAvailableForProductAsync(Guid productId)
        => _stockManager.GetGlobalAvailableQuantityForProductAsync(productId);

    public async Task<IPagedDataAppDto<StockMovementLogAppDto>> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize)
    {
        var (total, dataItems) = await _stockManager.GetStockMovementLogsAsync(productId, warehouseId, pageIndex, pageSize);

        var items = dataItems.Select(x => new StockMovementLogAppDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            MovementType = x.MovementType,
            Quantity = x.Quantity,
            QuantityBefore = x.QuantityBefore,
            QuantityAfter = x.QuantityAfter,
            CreatedOnUtc = x.CreatedOnUtc,
            Note = x.Note
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, total);
    }

    public async Task<SetStockLevelsResultAppDto> SetStockLevelsAsync(SetStockLevelsAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new SetStockLevelsResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            await _stockManager.SetStockLevelsAsync(new SetStockLevelsDto(dto.Id)
            {
                ReorderLevel = dto.ReorderLevel,
                MaxStockLevel = dto.MaxStockLevel
            }).ConfigureAwait(false);
        }
        catch (StockNotFoundException ex)
        {
            return new SetStockLevelsResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (InvalidStockOperationException ex)
        {
            return new SetStockLevelsResultAppDto { Success = false, ErrorMessage = ex.Message };
        }

        return new SetStockLevelsResultAppDto { Success = true, UpdatedId = dto.Id };
    }

    public async Task<IPagedDataAppDto<ProductReservationLedgerAppDto>> GetProductReservationLedgerAsync(Guid productId, int pageIndex, int pageSize)
    {
        var product = await _productReader.GetByIdAsync(productId).ConfigureAwait(false);
        var query = _reservationLedgerReader.DataSource
            .Where(x => x.ProductId == productId)
            .OrderByDescending(x => x.CreatedOnUtc);

        var total = query.Count();
        var entries = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        var orderIds = entries.Select(x => x.OrderId).Distinct().ToList();
        var orderCodes = _orderReader.DataSource
            .Where(x => orderIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.Code);

        var items = entries.Select(x => new ProductReservationLedgerAppDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = product?.Name ?? string.Empty,
            OrderId = x.OrderId,
            OrderCode = orderCodes.GetValueOrDefault(x.OrderId),
            QuantityDelta = x.QuantityDelta,
            Reason = (int)x.Reason,
            ReferenceId = x.ReferenceId,
            CreatedOnUtc = x.CreatedOnUtc
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, total);
    }
}
