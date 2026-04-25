using System.Diagnostics;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class InventoryStockManager : IInventoryStockManager
{
    private readonly IStockAuditLogger _stockAuditLogger;
    private readonly IRepository<InventoryStock> _inventoryStockRepository;
    private readonly IRepository<StockMovementLog> _stockMovementRepository;
    private readonly IEntityDataReader<StockMovementLog> _stockMovementDataReader;
    private readonly IEntityDataReader<InventoryStock> _inventoryStockDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;

    public InventoryStockManager(IRepository<InventoryStock> inventoryStockRepository, IEntityDataReader<InventoryStock> inventoryStockDataReader, IStockAuditLogger stockAuditLogger, IRepository<StockMovementLog> stockMovementRepository, IEntityDataReader<Product> productDataReader, IEntityDataReader<Warehouse> warehouseDataReader, IEntityDataReader<StockMovementLog> stockMovementDataReader)
    {
        _inventoryStockRepository = inventoryStockRepository;
        _inventoryStockDataReader = inventoryStockDataReader;
        _stockAuditLogger = stockAuditLogger;
        _stockMovementRepository = stockMovementRepository;
        _productDataReader = productDataReader;
        _warehouseDataReader = warehouseDataReader;
        _stockMovementDataReader = stockMovementDataReader;
    }

    public async Task<InventoryStock> InitializeStockAsync(Guid productId, Guid warehouseId, Guid unitMeasurementId)
    {
        var stock = new InventoryStock(Guid.NewGuid(), productId, warehouseId, unitMeasurementId);
        await _inventoryStockRepository.InsertAsync(stock).ConfigureAwait(false);
        return stock;
    }

    public async Task<StockMovementLogDto?> AdjustStockAsync(Guid productId, Guid warehouseId, decimal newQuantity, string? note, Guid modifiedByUserId)
    {
        if (newQuantity < 0)
            throw new InvalidStockOperationException("Error.StockQuantityCannotBeNegative");

        var product = await _productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(warehouseId);

        var stock = await GetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            stock = await InitializeStockAsync(productId, warehouseId, product.UnitMeasurementId ?? Guid.Empty).ConfigureAwait(false);

        var quantityBefore = stock.QuantityOnHand;
        var quantityChange = newQuantity - quantityBefore;
        if (quantityChange == 0)
            return null;

        // Validate against max stock level if increasing
        if (newQuantity > quantityBefore && stock.MaxStockLevel > 0 && newQuantity > stock.MaxStockLevel)
            throw new WarehouseCapacityExceededException("Error.WarehouseCapacityExceeded", stock.MaxStockLevel, newQuantity);

        stock.QuantityOnHand = newQuantity;
        stock.UpdatedOnUtc = DateTime.UtcNow;
        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        var movementType = quantityChange > 0 ? StockMovementType.Adjustment : StockMovementType.Adjustment;
        var stockMovementLog = new StockMovementLog(Guid.NewGuid(), stock.ProductId, stock.WarehouseId,
            movementType, Math.Abs(quantityChange), quantityBefore, stock.QuantityOnHand,
            0, null, note, modifiedByUserId
        );
        await _stockMovementRepository.InsertAsync(stockMovementLog).ConfigureAwait(false);

        await _stockAuditLogger.LogStockOperationAsync(productId, product.Name,
            warehouseId, warehouse.Name, "Điều chỉnh", Math.Abs(quantityChange), quantityBefore, stock.QuantityOnHand,
            modifiedByUserId, null, note, Activity.Current?.Id);

        return new StockMovementLogDto(stockMovementLog.Id)
        {
            ProductId = stockMovementLog.ProductId,
            ProductName = product.Name,
            MovementType = (int)stockMovementLog.MovementType,
            Quantity = stockMovementLog.Quantity,
            QuantityBefore = stockMovementLog.QuantityBefore,
            QuantityAfter = stockMovementLog.QuantityAfter,
            CreatedOnUtc = stockMovementLog.CreatedOnUtc,
            Note = stockMovementLog.Note
        };
    }

    public async Task<StockMovementLogDto?> ReceiveStockAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId)
    {
        if (receivedQuantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var product = await _productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(warehouseId);

        var stock = await GetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            stock = await InitializeStockAsync(productId, warehouseId, product.UnitMeasurementId ?? Guid.Empty).ConfigureAwait(false);

        var quantityBefore = stock.QuantityOnHand;
        var quantityAfter = quantityBefore + receivedQuantity;

        // Validate against max stock level if set
        if (stock.MaxStockLevel > 0 && quantityAfter > stock.MaxStockLevel)
            throw new WarehouseCapacityExceededException("Error.WarehouseCapacityExceeded", stock.MaxStockLevel, quantityAfter);

        stock.QuantityOnHand = quantityAfter;
        stock.UpdatedOnUtc = DateTime.UtcNow;
        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        var stockMovementLog = new StockMovementLog(
            Guid.NewGuid(),
            stock.ProductId,
            stock.WarehouseId,
            StockMovementType.Inbound,
            receivedQuantity,
            quantityBefore,
            quantityAfter,
            (StockReferenceType)referenceType,
            referenceId,
            note,
            receivedByUserId
        );
        await _stockMovementRepository.InsertAsync(stockMovementLog, CancellationToken.None);

        return new StockMovementLogDto(stockMovementLog.Id)
        {
            ProductId = stockMovementLog.ProductId,
            ProductName = product.Name,
            MovementType = (int)stockMovementLog.MovementType,
            Quantity = stockMovementLog.Quantity,
            QuantityBefore = stockMovementLog.QuantityBefore,
            QuantityAfter = stockMovementLog.QuantityAfter,
            CreatedOnUtc = stockMovementLog.CreatedOnUtc,
            Note = stockMovementLog.Note
        };
    }

    public async Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(string? keywords, Guid? warehouseId, int pageIndex, int pageSize)
    {
        var inventoryStockQuery = _inventoryStockDataReader.DataSource;
        if (warehouseId.HasValue)
            inventoryStockQuery = inventoryStockQuery.Where(x => x.WarehouseId == warehouseId);

        var productQuery = _productDataReader.DataSource;
        var warehouseQuery = _warehouseDataReader.DataSource;

        var query = from s in inventoryStockQuery
                    join p in productQuery on s.ProductId equals p.Id
                    join w in warehouseQuery on s.WarehouseId equals w.Id
                    select new { s, p, w };

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);
            var uppercaseKeywords = keywords.Trim().ToUpper();
            query = query.Where(agg => agg.p.Name.ToUpper().Contains(uppercaseKeywords) || agg.p.Name.ToUpper().Contains(normalizedKeywords) || agg.p.NormalizedName.Contains(normalizedKeywords)
                || agg.w.Name.ToUpper().Contains(uppercaseKeywords) || agg.w.Name.ToUpper().Contains(normalizedKeywords) || agg.w.NormalizedName.Contains(normalizedKeywords));
        }

        var total = query.Count();

        var items = query
            .OrderBy(x => x.p.Name)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => new InventoryStockDto(x.s.Id)
            {
                ProductId = x.s.ProductId,
                ProductName = x.p.Name,
                WarehouseId = x.s.WarehouseId,
                WarehouseName = x.w.Name,
                QuantityOnHand = x.s.QuantityOnHand,
                QuantityReserved = x.s.QuantityReserved,
                QuantityAvailable = x.s.QuantityOnHand - x.s.QuantityReserved,
                UpdatedOnUtc = x.s.UpdatedOnUtc
            })
            .ToList();

        return (total, items);
    }

    public async Task<IEnumerable<InventoryStockDto>> GetInventoryStocksForProductAsync(Guid productId, Guid? warehouseId)
    {
        var inventoryStockQuery = _inventoryStockDataReader.DataSource;
        inventoryStockQuery = inventoryStockQuery.Where(x => x.ProductId == productId);
        if (warehouseId.HasValue)
            inventoryStockQuery = inventoryStockQuery.Where(x => x.WarehouseId == warehouseId);

        var productQuery = _productDataReader.DataSource;
        var warehouseQuery = _warehouseDataReader.DataSource;

        var query = from s in inventoryStockQuery
                    join p in productQuery on s.ProductId equals p.Id
                    join w in warehouseQuery on s.WarehouseId equals w.Id
                    select new { s, p, w };
        var items = query
            .Select(x => new InventoryStockDto(x.s.Id)
            {
                ProductId = x.s.ProductId,
                ProductName = x.p.Name,
                WarehouseId = x.s.WarehouseId,
                WarehouseName = x.w.Name,
                QuantityOnHand = x.s.QuantityOnHand,
                QuantityReserved = x.s.QuantityReserved,
                QuantityAvailable = x.s.QuantityOnHand - x.s.QuantityReserved,
                UpdatedOnUtc = x.s.UpdatedOnUtc
            })
            .OrderByDescending(x => x.UpdatedOnUtc)
            .ThenBy(x => x.ProductName)
            .ToList();

        return items;
    }

    public async Task<bool> ReserveStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, int? reservationDaysValid = null)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var stock = await GetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            stock = await InitializeStockAsync(productId, warehouseId, Guid.Empty);

        // Auto-release expired reservations before checking availability
        if (stock.ReservedUntilUtc.HasValue && stock.ReservedUntilUtc < DateTime.UtcNow)
        {
            stock.QuantityReserved = 0;
            stock.ReservedUntilUtc = null;
        }

        // Check availability - cannot exceed available quantity
        if (stock.QuantityAvailable < quantity)
            throw new InsufficientStockException(productId, warehouseId, quantity, stock.QuantityAvailable);

        stock.QuantityReserved += quantity;

        // Set expiration date (default 7 days if not specified)
        var daysValid = reservationDaysValid ?? 7;
        stock.ReservedUntilUtc = DateTime.UtcNow.AddDays(daysValid);

        stock.UpdatedOnUtc = DateTime.UtcNow;

        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> ReleaseReservedStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var stock = await GetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            throw new StockNotFoundException("Error.StockNotFound", productId, warehouseId);

        // Cannot release more than what's reserved
        if (stock.QuantityReserved < quantity)
            throw new InvalidStockOperationException("Error.CannotReleaseMoreThanReserved", stock.QuantityReserved, quantity);

        stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - quantity);
        stock.UpdatedOnUtc = DateTime.UtcNow;

        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);
        return true;
    }

    public async Task<StockMovementLogDto?> DispatchStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null)
    {
        if (quantity <= 0) return null;

        var product = await _productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(warehouseId);

        var stock = await GetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null) 
            return null;

        // Validate sufficient quantity available
        if (stock.QuantityOnHand < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {stock.QuantityOnHand}, Requested: {quantity}");

        var quantityBefore = stock.QuantityOnHand;

        // When dispatching, only reduce QuantityOnHand (physical stock leaving warehouse)
        // If this dispatch is for a reserved order, the reserved allocation is cleared proportionally
        stock.QuantityOnHand -= quantity;

        // Clear reserved allocation if dispatching from reserved stock
        if (stock.QuantityReserved > 0)
            stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - quantity);

        stock.UpdatedOnUtc = DateTime.UtcNow;
        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        var log = new StockMovementLog(
            Guid.NewGuid(),
            stock.ProductId,
            stock.WarehouseId,
            StockMovementType.Outbound,
            quantity,
            quantityBefore,
            stock.QuantityOnHand,
            StockReferenceType.SalesOrder,
            referenceId,
            note,
            userId
        );
        await _stockMovementRepository.InsertAsync(log).ConfigureAwait(false);

        return new StockMovementLogDto(log.Id)
        {
            ProductId = log.ProductId,
            ProductName = product.Name,
            MovementType = (int)log.MovementType,
            Quantity = log.Quantity,
            QuantityBefore = log.QuantityBefore,
            QuantityAfter = log.QuantityAfter,
            CreatedOnUtc = log.CreatedOnUtc,
            Note = log.Note
        };
    }

    public async Task<(int Total, List<StockMovementLogDto> Items)> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize)
    {
        var stockMovementLogQuery = _stockMovementDataReader.DataSource;

        if (warehouseId.HasValue)
            stockMovementLogQuery = stockMovementLogQuery.Where(x => x.WarehouseId == warehouseId);

        if (productId.HasValue)
            stockMovementLogQuery = stockMovementLogQuery.Where(x => x.ProductId == productId);

        var productQuery = _productDataReader.DataSource;

        var query = from l in stockMovementLogQuery
                    join p in productQuery on l.ProductId equals p.Id
                    select new { l, p };

        var total = query.Count();

        var items = query
            .OrderByDescending(x => x.l.CreatedOnUtc)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .Select(x => new StockMovementLogDto(x.l.Id)
            {
                ProductId = x.l.ProductId,
                ProductName = x.p.Name,
                MovementType = (int)x.l.MovementType,
                Quantity = x.l.Quantity,
                QuantityBefore = x.l.QuantityBefore,
                QuantityAfter = x.l.QuantityAfter,
                CreatedOnUtc = x.l.CreatedOnUtc,
                Note = x.l.Note
            })
            .ToList();

        return (total, items);
    }

    public async Task<int> ReleaseExpiredReservationsAsync()
    {
        var now = DateTime.UtcNow;
        int releasedCount = 0;

        var expiredStocks = await Task.Run(() => _inventoryStockDataReader.DataSource.Where(s =>
            s.QuantityReserved > 0 &&
            s.ReservedUntilUtc.HasValue &&
            s.ReservedUntilUtc < now).ToList());

        foreach (var stock in expiredStocks)
        {
            stock.QuantityReserved = 0;
            stock.ReservedUntilUtc = null;
            stock.UpdatedOnUtc = now;

            await _inventoryStockRepository.UpdateAsync(stock, CancellationToken.None).ConfigureAwait(false);
            releasedCount++;
        }

        return releasedCount;
    }

    /// <summary>
    /// Check if stock quantity has fallen below reorder level
    /// Returns tuple: (isLowStock, reorderLevel)
    /// </summary>
    public (bool IsLowStock, decimal ReorderLevel) IsLowStock(InventoryStock stock)
    {
        if (stock.ReorderLevel <= 0)
            return (false, stock.ReorderLevel); // No reorder level set

        return (stock.QuantityOnHand < stock.ReorderLevel, stock.ReorderLevel);
    }

    /// <summary>
    /// Check if stock quantity exceeds maximum level
    /// Returns tuple: (isOverstocked, maxLevel)
    /// </summary>
    public (bool IsOverstocked, decimal MaxLevel) IsOverstocked(InventoryStock stock)
    {
        if (stock.MaxStockLevel <= 0)
            return (false, stock.MaxStockLevel); // No max level set

        return (stock.QuantityOnHand > stock.MaxStockLevel, stock.MaxStockLevel);
    }

    private Task<InventoryStock?> GetInventoryStockForProductAsync(Guid productId, Guid warehouseId)
    {
        return Task.Run(() => (from inventoryStock in _inventoryStockDataReader.DataSource
                               where inventoryStock.ProductId == productId && inventoryStock.WarehouseId == warehouseId
                               select inventoryStock).SingleOrDefault());
    }
}
