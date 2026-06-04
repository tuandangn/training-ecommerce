using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Inventory;

public sealed class InventoryAppService : IInventoryAppService
{
    private const int StockListCostHistorySize = 20;

    private readonly IInventoryStockManager _stockManager;
    private readonly IEntityDataReader<ProductReservationLedger> _reservationLedgerReader;
    private readonly IEntityDataReader<Product> _productReader;
    private readonly IEntityDataReader<Order> _orderReader;
    private readonly IEntityDataReader<Warehouse> _warehouseReader;
    private readonly IEntityDataReader<InventoryCostLedgerEntry> _costLedgerReader;
    private readonly IEntityDataReader<StockMovementLog> _stockMovementReader;
    private readonly IInventoryCostingManager _inventoryCostingManager;

    public InventoryAppService(
        IInventoryStockManager stockManager,
        IEntityDataReader<ProductReservationLedger> reservationLedgerReader,
        IEntityDataReader<Product> productReader,
        IEntityDataReader<Order> orderReader,
        IEntityDataReader<Warehouse> warehouseReader,
        IEntityDataReader<InventoryCostLedgerEntry> costLedgerReader,
        IEntityDataReader<StockMovementLog> stockMovementReader,
        IInventoryCostingManager inventoryCostingManager)
    {
        _stockManager = stockManager;
        _reservationLedgerReader = reservationLedgerReader;
        _productReader = productReader;
        _orderReader = orderReader;
        _warehouseReader = warehouseReader;
        _costLedgerReader = costLedgerReader;
        _stockMovementReader = stockMovementReader;
        _inventoryCostingManager = inventoryCostingManager;
    }

    public async Task<IPagedDataAppDto<InventoryStockAppDto>> GetInventoryStocksAsync(int pageIndex, int pageSize,
        Guid? warehouseId = null, Guid? productId = null, string? keywords = null, bool includeDirectTransit = false)
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(warehouseId, includeDirectTransit).ConfigureAwait(false);

        var (total, dataItems) = await _stockManager.GetInventoryStocksAsync(pageIndex, pageSize, warehouseIds, [productId], keywords).ConfigureAwait(false);
        var productIds = dataItems.Select(x => x.ProductId).Distinct().ToList();
        var rowWarehouseIds = dataItems.Select(x => x.WarehouseId).Distinct().ToList();
        var reservedByOrder = GetReservedByOrder(productIds);
        var currentCostByProduct = await GetCurrentCostByProductAsync(productIds).ConfigureAwait(false);
        var costHistoryByStock = GetCostHistoryByStock(productIds, rowWarehouseIds, StockListCostHistorySize);

        var items = dataItems.Select(x => MapInventoryStock(
            x,
            reservedByOrder,
            currentCostByProduct,
            costHistoryByStock.GetValueOrDefault((x.ProductId, x.WarehouseId), []))).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, total);
    }

    public async Task<IPagedDataAppDto<InventoryStockByProductAppDto>> GetInventoryStocksGroupedByProductAsync(
        int pageIndex,
        int pageSize,
        Guid? warehouseId = null,
        string? keywords = null,
        bool includeDirectTransit = false)
    {
        var warehouseIds = await ResolveWarehouseIdsAsync(warehouseId, includeDirectTransit).ConfigureAwait(false);
        var (_, stockRows) = await _stockManager.GetInventoryStocksAsync(0, int.MaxValue, warehouseIds, null, keywords).ConfigureAwait(false);
        var allProductIds = stockRows.Select(x => x.ProductId).Distinct().ToList();
        var reservedByOrder = GetReservedByOrder(allProductIds);
        var currentCostByProduct = await GetCurrentCostByProductAsync(allProductIds).ConfigureAwait(false);

        var groupedRows = stockRows
            .GroupBy(x => x.ProductId)
            .OrderBy(g => g.First().ProductName)
            .ToList();

        var total = groupedRows.Count;
        var pageGroups = groupedRows
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();
        var pageProductIds = pageGroups.Select(g => g.Key).ToList();
        var pageWarehouseIds = pageGroups.SelectMany(g => g.Select(x => x.WarehouseId)).Distinct().ToList();
        var costHistoryByProduct = GetCostHistoryByProduct(pageProductIds, StockListCostHistorySize);
        var costHistoryByStock = GetCostHistoryByStock(pageProductIds, pageWarehouseIds, StockListCostHistorySize);

        var items = pageGroups.Select(group =>
        {
            var warehouses = group
                .OrderBy(x => x.WarehouseName)
                .Select(x => MapInventoryStock(
                    x,
                    reservedByOrder,
                    currentCostByProduct,
                    costHistoryByStock.GetValueOrDefault((x.ProductId, x.WarehouseId), [])))
                .ToList();
            var first = warehouses.First();

            return new InventoryStockByProductAppDto
            {
                ProductId = group.Key,
                ProductName = first.ProductName,
                QuantityOnHand = warehouses.Sum(x => x.QuantityOnHand),
                QuantityReserved = warehouses.Sum(x => x.QuantityReserved),
                TotalReservedByOrder = reservedByOrder.GetValueOrDefault(group.Key),
                QuantityAvailable = warehouses.Sum(x => x.QuantityAvailable),
                CurrentUnitCost = currentCostByProduct.GetValueOrDefault(group.Key),
                UpdatedOnUtc = warehouses.Max(x => x.UpdatedOnUtc),
                Warehouses = warehouses,
                CostHistory = costHistoryByProduct.GetValueOrDefault(group.Key, [])
            };
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, total);
    }

    public Task<IReadOnlyList<InventoryCostHistoryAppDto>> GetInventoryCostHistoryAsync(Guid productId, Guid? warehouseId = null, int take = StockListCostHistorySize)
        => Task.FromResult(GetCostHistory(productId, warehouseId, take));

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
        var orders = _orderReader.DataSource
            .Where(x => orderIds.Contains(x.Id))
            .ToList();
        var orderMap = orders.ToDictionary(x => x.Id);

        var items = entries.Select(x => new ProductReservationLedgerAppDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = product?.Name ?? string.Empty,
            OrderId = x.OrderId,
            OrderCode = orderMap.GetValueOrDefault(x.OrderId)?.Code,
            QuantityDelta = x.QuantityDelta,
            UnitPrice = ResolveReservationUnitPrice(x, orderMap),
            Reason = (int)x.Reason,
            ReferenceId = x.ReferenceId,
            CreatedOnUtc = x.CreatedOnUtc
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, total);
    }

    private static decimal? ResolveReservationUnitPrice(ProductReservationLedger entry, IReadOnlyDictionary<Guid, Order> orderMap)
    {
        if (!orderMap.TryGetValue(entry.OrderId, out var order))
            return null;

        var referencedItem = entry.ReferenceId.HasValue
            ? order.OrderItems.FirstOrDefault(item => item.Id == entry.ReferenceId.Value)
            : null;
        if (referencedItem is not null)
            return referencedItem.UnitPrice;

        var productItems = order.OrderItems
            .Where(item => item.ProductId == entry.ProductId)
            .Take(2)
            .ToList();

        return productItems.Count == 1 ? productItems[0].UnitPrice : null;
    }

    private async Task<Guid?[]?> ResolveWarehouseIdsAsync(Guid? warehouseId, bool includeDirectTransit)
    {
        if (warehouseId.HasValue)
            return [warehouseId.Value];

        var warehouses = await _warehouseReader.GetAllAsync().ConfigureAwait(false);
        return includeDirectTransit
            ? warehouses.Select(warehouse => (Guid?)warehouse.Id).ToArray()
            : warehouses.Where(warehouse => warehouse.WarehouseType != WarehouseType.DirectTransit).Select(warehouse => (Guid?)warehouse.Id).ToArray();
    }

    private Dictionary<Guid, decimal> GetReservedByOrder(IReadOnlyCollection<Guid> productIds)
    {
        if (productIds.Count == 0)
            return [];

        return _reservationLedgerReader.DataSource
            .Where(x => productIds.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityDelta));
    }

    private async Task<Dictionary<Guid, decimal>> GetCurrentCostByProductAsync(IReadOnlyCollection<Guid> productIds)
    {
        var currentCostByProduct = new Dictionary<Guid, decimal>();
        foreach (var id in productIds)
        {
            var costSummary = await _inventoryCostingManager.GetCurrentCostSummaryAsync(id).ConfigureAwait(false);
            currentCostByProduct[id] = costSummary.AverageCost;
        }

        return currentCostByProduct;
    }

    private static InventoryStockAppDto MapInventoryStock(
        InventoryStockDto x,
        IReadOnlyDictionary<Guid, decimal> reservedByOrder,
        IReadOnlyDictionary<Guid, decimal> currentCostByProduct,
        IReadOnlyList<InventoryCostHistoryAppDto> costHistory)
        => new()
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
            CurrentUnitCost = currentCostByProduct.GetValueOrDefault(x.ProductId),
            UpdatedOnUtc = x.UpdatedOnUtc,
            ReorderLevel = x.ReorderLevel,
            MaxStockLevel = x.MaxStockLevel,
            CostHistory = costHistory
        };

    private IReadOnlyList<InventoryCostHistoryAppDto> GetCostHistory(Guid productId, Guid? warehouseId, int take)
    {
        var safeTake = Math.Max(1, take);
        var query = _costLedgerReader.DataSource
            .Where(x => x.ProductId == productId && x.CostingStatus != InventoryCostingStatus.Superseded);

        if (warehouseId.HasValue)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        var entries = query
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.SequenceNumber)
            .Take(safeTake)
            .ToList();

        return MapCostHistory(entries);
    }

    private IReadOnlyDictionary<(Guid ProductId, Guid WarehouseId), IReadOnlyList<InventoryCostHistoryAppDto>> GetCostHistoryByStock(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyCollection<Guid> warehouseIds,
        int take)
    {
        if (productIds.Count == 0 || warehouseIds.Count == 0)
            return new Dictionary<(Guid ProductId, Guid WarehouseId), IReadOnlyList<InventoryCostHistoryAppDto>>();

        var entries = _costLedgerReader.DataSource
            .Where(x => productIds.Contains(x.ProductId)
                && warehouseIds.Contains(x.WarehouseId)
                && x.CostingStatus != InventoryCostingStatus.Superseded)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.SequenceNumber)
            .ToList();

        return MapCostHistory(entries)
            .GroupBy(x => (x.ProductId, x.WarehouseId))
            .ToDictionary(g => g.Key, g => (IReadOnlyList<InventoryCostHistoryAppDto>)g.Take(take).ToList());
    }

    private IReadOnlyDictionary<Guid, IReadOnlyList<InventoryCostHistoryAppDto>> GetCostHistoryByProduct(
        IReadOnlyCollection<Guid> productIds,
        int take)
    {
        if (productIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<InventoryCostHistoryAppDto>>();

        var entries = _costLedgerReader.DataSource
            .Where(x => productIds.Contains(x.ProductId)
                && x.CostingStatus != InventoryCostingStatus.Superseded)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.SequenceNumber)
            .ToList();

        return MapCostHistory(entries)
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<InventoryCostHistoryAppDto>)g.Take(take).ToList());
    }

    public async Task<string?> GetReturnWarehouseNameForDeliveryNoteAsync(Guid deliveryNoteId, Guid deliveryNoteWarehouseId)
    {
        var returnWarehouseId = _stockMovementReader.DataSource
            .Where(log => log.ReferenceId == deliveryNoteId
                          && log.ReferenceType == StockReferenceType.StockTransfer
                          && log.WarehouseId != deliveryNoteWarehouseId)
            .Select(log => log.WarehouseId)
            .FirstOrDefault();

        if (returnWarehouseId == default)
            return null;

        var name = _warehouseReader.DataSource
            .Where(w => w.Id == returnWarehouseId)
            .Select(w => w.Name)
            .FirstOrDefault();

        return name.Value;
    }

    private IReadOnlyList<InventoryCostHistoryAppDto> MapCostHistory(IReadOnlyCollection<InventoryCostLedgerEntry> entries)
    {
        if (entries.Count == 0)
            return [];

        var productIds = entries.Select(x => x.ProductId).Distinct().ToList();
        var warehouseIds = entries.Select(x => x.WarehouseId).Distinct().ToList();
        var productMap = _productReader.DataSource
            .Where(x => productIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.Name);
        var warehouseMap = _warehouseReader.DataSource
            .Where(x => warehouseIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.Name);

        return entries.Select(x => new InventoryCostHistoryAppDto
        {
            Id = x.Id,
            ProductId = x.ProductId,
            ProductName = productMap.GetValueOrDefault(x.ProductId) ?? string.Empty,
            WarehouseId = x.WarehouseId,
            WarehouseName = warehouseMap.GetValueOrDefault(x.WarehouseId).Value ?? string.Empty,
            OccurredAtUtc = x.OccurredAtUtc,
            SequenceNumber = x.SequenceNumber,
            MovementType = (int)x.MovementType,
            QuantityDelta = x.QuantityDelta,
            UnitCost = x.UnitCost,
            TotalCost = x.TotalCost,
            QuantityBalanceAfter = x.QuantityBalanceAfter,
            ValueBalanceAfter = x.ValueBalanceAfter,
            AverageCostAfter = x.AverageCostAfter,
            CostingStatus = (int)x.CostingStatus,
            ReferenceType = (int)x.ReferenceType,
            ReferenceId = x.ReferenceId,
            ReferenceItemId = x.ReferenceItemId
        }).ToList();
    }
}
