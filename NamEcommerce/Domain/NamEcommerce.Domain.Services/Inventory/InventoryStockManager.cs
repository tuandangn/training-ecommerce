using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;
using System.Diagnostics;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class InventoryStockManager : IInventoryStockManager
{
    private readonly IDbContext _dbContext;
    private readonly IStockAuditLogger _auditLogger;

    public InventoryStockManager(IDbContext dbContext, IStockAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _auditLogger = auditLogger;
    }

    public async Task<InventoryStock> InitializeStockAsync(Guid productId, Guid warehouseId, Guid unitMeasurementId)
    {
        var stock = new InventoryStock(Guid.NewGuid(), productId, warehouseId, unitMeasurementId);
        await _dbContext.AddAsync(stock, CancellationToken.None);
        return stock;
    }

    public async Task<StockMovementLogDto?> AdjustStockAsync(
        Guid productId,
        Guid warehouseId,
        decimal newQuantity, 
        string? note, 
        Guid modifiedByUserId)
    {
        if (newQuantity < 0)
            throw new InvalidStockOperationException("Stock quantity cannot be negative");

        var stockList = await _dbContext.GetDataAsync<InventoryStock>();
        var stock = stockList.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);

        if (stock == null)
        {
            stock = await InitializeStockAsync(productId, warehouseId, Guid.Empty);
        }

        var quantityBefore = stock.QuantityOnHand;
        var quantityChange = newQuantity - quantityBefore;

        if (quantityChange == 0)
        {
            return null;
        }

        // Validate against max stock level if increasing
        if (newQuantity > quantityBefore && stock.MaxStockLevel > 0 && newQuantity > stock.MaxStockLevel)
            throw new WarehouseCapacityExceededException(
                $"Adjustment exceeds maximum stock level. Max: {stock.MaxStockLevel}, Adjusted: {newQuantity}");

        stock.QuantityOnHand = newQuantity;
        stock.UpdatedOnUtc = DateTime.UtcNow;

        var movementType = quantityChange > 0 ? StockMovementType.Adjustment : StockMovementType.Adjustment;

        var log = new StockMovementLog(
            Guid.NewGuid(),
            stock.ProductId,
            stock.WarehouseId,
            movementType,
            Math.Abs(quantityChange),
            quantityBefore,
            stock.QuantityOnHand,
            (StockReferenceType)0,
            null,
            note,
            modifiedByUserId
        );

        await _dbContext.UpdateAsync(stock, CancellationToken.None);
        await _dbContext.AddAsync(log, CancellationToken.None);

        var product = (await _dbContext.GetDataAsync<Product>()).FirstOrDefault(p => p.Id == productId);
        var warehouse = (await _dbContext.GetDataAsync<Warehouse>()).FirstOrDefault(w => w.Id == warehouseId);

        // #10 Audit Logging
        await _auditLogger.LogStockOperationAsync(
            productId,
            product?.Name ?? "Unknown",
            warehouseId,
            warehouse?.Name ?? "Unknown",
            "Adjust",
            Math.Abs(quantityChange),
            quantityBefore,
            stock.QuantityOnHand,
            modifiedByUserId,
            null,
            note,
            Activity.Current?.Id);

        return new StockMovementLogDto(log.Id)
        {
            ProductId = log.ProductId,
            ProductName = product?.Name ?? "Unknown",
            MovementType = (int)log.MovementType,
            Quantity = log.Quantity,
            QuantityBefore = log.QuantityBefore,
            QuantityAfter = log.QuantityAfter,
            CreatedOnUtc = log.CreatedOnUtc,
            Note = log.Note
        };
    }

    public async Task<StockMovementLogDto?> ReceiveStockAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId)
    {
        if (receivedQuantity <= 0)
            throw new InvalidStockOperationException("Received quantity must be greater than 0");

        var stockList = await _dbContext.GetDataAsync<InventoryStock>();
        var stock = stockList.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);

        if (stock == null)
            stock = await InitializeStockAsync(productId, warehouseId, Guid.Empty);

        var quantityBefore = stock.QuantityOnHand;
        var quantityAfter = quantityBefore + receivedQuantity;
        
        // Validate against max stock level if set
        if (stock.MaxStockLevel > 0 && quantityAfter > stock.MaxStockLevel)
            throw new WarehouseCapacityExceededException(
                $"Receipt would exceed maximum stock level. Max: {stock.MaxStockLevel}, Projected: {quantityAfter}");

        stock.QuantityOnHand = quantityAfter;
        stock.UpdatedOnUtc = DateTime.UtcNow;

        var log = new StockMovementLog(
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

        await _dbContext.UpdateAsync(stock, CancellationToken.None);
        await _dbContext.AddAsync(log, CancellationToken.None);

        var product = (await _dbContext.GetDataAsync<Product>()).FirstOrDefault(p => p.Id == productId);

        return new StockMovementLogDto(log.Id)
        {
            ProductId = log.ProductId,
            ProductName = product?.Name ?? "Unknown",
            MovementType = (int)log.MovementType,
            Quantity = log.Quantity,
            QuantityBefore = log.QuantityBefore,
            QuantityAfter = log.QuantityAfter,
            CreatedOnUtc = log.CreatedOnUtc,
            Note = log.Note
        };
    }

    public async Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(string? keywords, Guid? warehouseId, int pageIndex, int pageSize)
    {
        var stockQuery = _dbContext.GetDataSource<InventoryStock>();
        if (warehouseId.HasValue)
            stockQuery = stockQuery.Where(x => x.WarehouseId == warehouseId);

        var productQuery = _dbContext.GetDataSource<Product>();
        var warehouseQuery = _dbContext.GetDataSource<Warehouse>();

        var query = from s in stockQuery
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
        var stockQuery = _dbContext.GetDataSource<InventoryStock>();
        stockQuery = stockQuery.Where(x => x.ProductId == productId);
        if (warehouseId.HasValue)
            stockQuery = stockQuery.Where(x => x.WarehouseId == warehouseId);

        var productQuery = _dbContext.GetDataSource<Product>();
        var warehouseQuery = _dbContext.GetDataSource<Warehouse>();

        var query = from s in stockQuery
                    join p in productQuery on s.ProductId equals p.Id
                    join w in warehouseQuery on s.WarehouseId equals w.Id
                    select new { s, p, w };
        var items = query
            .OrderBy(x => x.p.Name)
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

        return items;
    }


    public async Task<bool> ReserveStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, int? reservationDaysValid = null)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Quantity must be greater than 0");

        var stockList = await _dbContext.GetDataAsync<InventoryStock>();
        var stock = stockList.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);

        if (stock == null)
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

        await _dbContext.UpdateAsync(stock, CancellationToken.None);
        return true;
    }

    public async Task<bool> ReleaseReservedStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Quantity must be greater than 0");

        var stockList = await _dbContext.GetDataAsync<InventoryStock>();
        var stock = stockList.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);

        if (stock == null)
            throw new StockNotFoundException($"Stock not found for product {productId} in warehouse {warehouseId}");

        // Cannot release more than what's reserved
        if (stock.QuantityReserved < quantity)
            throw new InvalidStockOperationException(
                $"Cannot release more than reserved. Reserved: {stock.QuantityReserved}, Requested: {quantity}");

        stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - quantity);
        stock.UpdatedOnUtc = DateTime.UtcNow;

        await _dbContext.UpdateAsync(stock, CancellationToken.None);
        return true;
    }

    public async Task<StockMovementLogDto?> DispatchStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null)
    {
        if (quantity <= 0) return null;

        var stockList = await _dbContext.GetDataAsync<InventoryStock>();
        var stock = stockList.FirstOrDefault(s => s.ProductId == productId && s.WarehouseId == warehouseId);

        if (stock == null) return null;

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

        await _dbContext.UpdateAsync(stock, CancellationToken.None);
        await _dbContext.AddAsync(log, CancellationToken.None);

        var product = (await _dbContext.GetDataAsync<Product>()).FirstOrDefault(p => p.Id == productId);

        return new StockMovementLogDto(log.Id)
        {
            ProductId = log.ProductId,
            ProductName = product?.Name ?? "Unknown",
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
        var logQuery = _dbContext.GetDataSource<StockMovementLog>();

        if (warehouseId.HasValue)
            logQuery = logQuery.Where(x => x.WarehouseId == warehouseId);

        if (productId.HasValue)
            logQuery = logQuery.Where(x => x.ProductId == productId);

        var productQuery = _dbContext.GetDataSource<Product>();

        var query = from l in logQuery
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
        var stockList = await _dbContext.GetDataAsync<InventoryStock>();
        var now = DateTime.UtcNow;
        
        int releasedCount = 0;
        var expiredStocks = stockList.Where(s => 
            s.QuantityReserved > 0 && 
            s.ReservedUntilUtc.HasValue && 
            s.ReservedUntilUtc < now).ToList();

        foreach (var stock in expiredStocks)
        {
            stock.QuantityReserved = 0;
            stock.ReservedUntilUtc = null;
            stock.UpdatedOnUtc = now;

            await _dbContext.UpdateAsync(stock, CancellationToken.None);
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
}
