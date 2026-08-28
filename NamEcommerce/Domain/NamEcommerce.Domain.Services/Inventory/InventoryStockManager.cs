using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class InventoryStockManager(IRepository<InventoryStock> inventoryStockRepository, IEntityDataReader<InventoryStock> inventoryStockDataReader, IStockAuditLogger stockAuditLogger, IRepository<StockMovementLog> stockMovementRepository, IRepository<StockReservationEntry> stockReservationRepository, IEntityDataReader<Product> productDataReader, IEntityDataReader<Warehouse> warehouseDataReader, IEntityDataReader<StockMovementLog> stockMovementDataReader, IEntityDataReader<ProductReservationLedger> productReservationDataReader, IEntityDataReader<StockReservationEntry> stockReservationDataReader) : IInventoryStockManager
{
    public async Task InitializeStockAsync(Guid productId, Guid warehouseId, Guid? unitMeasurementId = null)
        => await EnsureInitializeStockAsync(productId, warehouseId, unitMeasurementId).ConfigureAwait(false);

    private async Task<InventoryStock> EnsureInitializeStockAsync(Guid productId, Guid warehouseId, Guid? unitMeasurementId = null)
    {
        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is not null)
            return stock;

        stock = new InventoryStock(Guid.NewGuid(), productId, warehouseId, unitMeasurementId);
        await inventoryStockRepository.InsertAsync(stock).ConfigureAwait(false);

        return stock;
    }

    public Task<StockMovementLogDto?> ReceiveStockAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId, bool enforceCapacity = true)
    {
        return ExecuteWithStockRetryAsync(() =>
        {
            return ReceiveCoreAsync(productId, warehouseId, receivedQuantity, note, receivedByUserId, referenceType, referenceId, enforceCapacity);
        });
    }

    private async Task<StockMovementLogDto?> ReceiveCoreAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId, bool enforceCapacity = true)
    {
        if (receivedQuantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var product = await productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await warehouseDataReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(warehouseId);

        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            stock = await EnsureInitializeStockAsync(productId, warehouseId, product.UnitMeasurementId).ConfigureAwait(false);

        var quantityBefore = stock.QuantityOnHand;
        var quantityAfter = quantityBefore + receivedQuantity;

        if (enforceCapacity && stock.MaxStockLevel > 0 && quantityAfter > stock.MaxStockLevel)
            throw new WarehouseCapacityExceededException("Error.WarehouseCapacityExceeded", stock.MaxStockLevel, quantityAfter);

        stock.QuantityOnHand = quantityAfter;
        stock.UpdatedOnUtc = DateTime.UtcNow;
        await inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

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
        await stockMovementRepository.InsertAsync(stockMovementLog);

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

    public async Task<StockMovementLogDto?> ReceiveStockUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity,
        string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId, bool enforceCapacity = true)
    {
        if (targetQuantity <= 0)
            return null;

        var alreadyReceived = await GetMovedQuantityAsync(
            productId,
            warehouseId,
            StockMovementType.Inbound,
            (StockReferenceType)referenceType,
            referenceId).ConfigureAwait(false);
        var missingQuantity = targetQuantity - alreadyReceived;

        if (missingQuantity <= 0)
            return null;

        return await ReceiveStockAsync(productId, warehouseId, missingQuantity, note, receivedByUserId, referenceType, referenceId, enforceCapacity)
            .ConfigureAwait(false);
    }

    public Task<StockMovementLogDto?> RevertReceiveAsync(Guid productId, Guid warehouseId, decimal quantity, Guid goodsReceiptId, Guid modifiedByUserId)
        => ExecuteWithStockRetryAsync(() => RevertReceiveCoreAsync(productId, warehouseId, quantity, goodsReceiptId, modifiedByUserId));

    private async Task<StockMovementLogDto?> RevertReceiveCoreAsync(Guid productId, Guid warehouseId, decimal quantity, Guid goodsReceiptId, Guid modifiedByUserId)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var product = await productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await warehouseDataReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(warehouseId);

        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            throw new StockNotFoundException("Error.StockNotFound", productId, warehouseId);

        if (quantity > stock.QuantityAvailable)
            throw new InsufficientStockException(productId, warehouseId, quantity, stock.QuantityAvailable);

        var quantityBefore = stock.QuantityOnHand;
        var quantityAfter = quantityBefore - quantity;

        stock.QuantityOnHand = quantityAfter;
        stock.UpdatedOnUtc = DateTime.UtcNow;
        await inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        var stockMovementLog = new StockMovementLog(
            Guid.NewGuid(),
            stock.ProductId,
            stock.WarehouseId,
            StockMovementType.Revert,
            quantity,
            quantityBefore,
            quantityAfter,
            StockReferenceType.GoodsReceipt,
            goodsReceiptId,
            $"Hoàn tác nhập hàng từ phiếu nhập {goodsReceiptId}",
            modifiedByUserId
        );
        await stockMovementRepository.InsertAsync(stockMovementLog).ConfigureAwait(false);

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

    public async Task<StockMovementLogDto?> RevertReceiveUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity, Guid goodsReceiptId, Guid modifiedByUserId)
    {
        if (targetQuantity <= 0)
            return null;

        var alreadyReverted = await GetMovedQuantityAsync(
            productId,
            warehouseId,
            StockMovementType.Revert,
            StockReferenceType.GoodsReceipt,
            goodsReceiptId).ConfigureAwait(false);
        var missingQuantity = targetQuantity - alreadyReverted;

        if (missingQuantity <= 0)
            return null;

        return await RevertReceiveAsync(productId, warehouseId, missingQuantity, goodsReceiptId, modifiedByUserId).ConfigureAwait(false);
    }

    public Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(int pageIndex, int pageSize,
        Guid? warehouseId = null, Guid? productId = null, string? keywords = null)
        => GetInventoryStocksAsync(pageIndex, pageSize, [warehouseId], [productId], keywords: keywords);

    public async Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(int pageIndex, int pageSize, Guid?[]? warehouseIds = null, Guid?[]? productIds = null, string? keywords = null)
    {
        var inventoryStockQuery = inventoryStockDataReader.DataSource;
        var searchProductIds = productIds?.OfType<Guid>().ToArray() ?? [];
        var searchWarehouseIds = warehouseIds?.OfType<Guid>().ToArray() ?? [];
        if (searchProductIds.Length > 0)
            inventoryStockQuery = inventoryStockQuery.Where(x => searchProductIds.Contains(x.ProductId));
        if (searchWarehouseIds.Length > 0)
            inventoryStockQuery = inventoryStockQuery.Where(x => searchWarehouseIds.Contains(x.WarehouseId));

        var productQuery = productDataReader.DataSource;
        var warehouseQuery = warehouseDataReader.DataSource;

        var query = from s in inventoryStockQuery
                    join p in productQuery on s.ProductId equals p.Id
                    join w in warehouseQuery on s.WarehouseId equals w.Id
                    select new { s, p, w };

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);
            var uppercaseKeywords = keywords.Trim().ToUpper();
            query = query.Where(agg => agg.p.Name.ToUpper().Contains(uppercaseKeywords) || agg.p.Name.ToUpper().Contains(normalizedKeywords) || agg.p.NormalizedName.Contains(normalizedKeywords)
                || agg.w.Name.Value.ToUpper().Contains(uppercaseKeywords) || agg.w.Name.Value.ToUpper().Contains(normalizedKeywords) || agg.w.Name.NormalizedValue.Contains(normalizedKeywords));
        }

        var total = await query.CountAsync().ConfigureAwait(false);

        var items = await query
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
                UpdatedOnUtc = x.s.UpdatedOnUtc,
                ReorderLevel = x.s.ReorderLevel,
                MaxStockLevel = x.s.MaxStockLevel
            })
            .ToListAsync().ConfigureAwait(false);

        return (total, items);
    }

    public async Task<IEnumerable<InventoryStockDto>> GetInventoryStocksForProductAsync(Guid productId)
    {
        var inventoryStockQuery = inventoryStockDataReader.DataSource;
        inventoryStockQuery = inventoryStockQuery.Where(x => x.ProductId == productId);

        var productQuery = productDataReader.DataSource;
        var warehouseQuery = warehouseDataReader.DataSource;

        var query = from s in inventoryStockQuery
                    join p in productQuery on s.ProductId equals p.Id
                    join w in warehouseQuery on s.WarehouseId equals w.Id
                    select new { s, p, w };
        var items = await query
            .Select(x => new InventoryStockDto(x.s.Id)
            {
                ProductId = x.s.ProductId,
                ProductName = x.p.Name,
                WarehouseId = x.s.WarehouseId,
                WarehouseName = x.w.Name,
                QuantityOnHand = x.s.QuantityOnHand,
                QuantityReserved = x.s.QuantityReserved,
                QuantityAvailable = x.s.QuantityOnHand - x.s.QuantityReserved,
                UpdatedOnUtc = x.s.UpdatedOnUtc,
                ReorderLevel = x.s.ReorderLevel,
                MaxStockLevel = x.s.MaxStockLevel
            })
            .OrderByDescending(x => x.UpdatedOnUtc)
            .ThenBy(x => x.ProductName)
            .ToListAsync().ConfigureAwait(false);

        return items;
    }

    public Task<decimal> GetGlobalOnHandQuantityForProductAsync(Guid productId)
    {
        var stockQuery = from s in inventoryStockDataReader.DataSource
                         join w in warehouseDataReader.DataSource on s.WarehouseId equals w.Id
                         where s.ProductId == productId && w.WarehouseType == WarehouseType.Physical
                         select s;
        return stockQuery.SumAsync(x => x.QuantityOnHand);
    }

    public async Task<decimal> GetGlobalAvailableQuantityForProductAsync(Guid productId, Guid? excludeOrderId = null)
    {
        var stockQuery = from s in inventoryStockDataReader.DataSource
                         join w in warehouseDataReader.DataSource on s.WarehouseId equals w.Id
                         where s.ProductId == productId && w.WarehouseType == WarehouseType.Physical
                         select s;
        var quantityOnHand = await stockQuery.SumAsync(x => x.QuantityOnHand).ConfigureAwait(false);
        var quantityReservedByWarehouse = await stockQuery.SumAsync(x => x.QuantityReserved).ConfigureAwait(false);
        var quantityReservedByOrder = await productReservationDataReader.DataSource
            .Where(x => x.ProductId == productId && (excludeOrderId == null || x.OrderId != excludeOrderId))
            .SumAsync(x => x.QuantityDelta)
            .ConfigureAwait(false);

        return quantityOnHand - quantityReservedByWarehouse - quantityReservedByOrder;
    }

    public async Task<decimal> ComputeAvailableQuantityForOrderAsync(Guid productId, Guid orderId)
    {
        var globalAvailable = await GetGlobalAvailableQuantityForProductAsync(productId).ConfigureAwait(false);
        var reservedForOrder = await productReservationDataReader.DataSource
            .Where(x => x.ProductId == productId && x.OrderId == orderId)
            .SumAsync(x => x.QuantityDelta).ConfigureAwait(false);

        return Math.Max(0, globalAvailable + reservedForOrder);
    }

    public Task<bool> ReserveStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, int? reservationDaysValid = null)
        => ExecuteWithStockRetryAsync(() => ReserveStockCoreAsync(productId, warehouseId, quantity, referenceId, userId, note));

    private async Task<bool> ReserveStockCoreAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            stock = await EnsureInitializeStockAsync(productId, warehouseId);

        if (stock.QuantityAvailable < quantity)
            throw new InsufficientStockException(productId, warehouseId, quantity, stock.QuantityAvailable);

        stock.QuantityReserved += quantity;
        stock.UpdatedOnUtc = DateTime.UtcNow;

        await inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        if (referenceId.HasValue)
            await stockReservationRepository.InsertAsync(new StockReservationEntry(productId, warehouseId, quantity, referenceId.Value, note)).ConfigureAwait(false);

        return true;
    }

    public Task<bool> ReleaseReservedStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null)
        => ExecuteWithStockRetryAsync(() => ReleaseReservedStockCoreAsync(productId, warehouseId, quantity, referenceId, userId, note));

    private async Task<bool> ReleaseReservedStockCoreAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            return false;

        var reservedForRef = referenceId.HasValue
            ? await stockReservationDataReader.DataSource
                .Where(e => e.ProductId == productId && e.WarehouseId == warehouseId && e.ReferenceId == referenceId.Value)
                .SumAsync(e => e.QuantityDelta).ConfigureAwait(false)
            : quantity;

        var toRelease = Math.Min(quantity, Math.Max(0, reservedForRef));
        if (toRelease <= 0)
            return true;

        stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - toRelease);
        stock.UpdatedOnUtc = DateTime.UtcNow;
        await inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        if (referenceId.HasValue)
            await stockReservationRepository.InsertAsync(new StockReservationEntry(productId, warehouseId, -toRelease, referenceId.Value, note)).ConfigureAwait(false);

        return true;
    }

    public Task<StockMovementLogDto?> DispatchStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, bool releaseReservedStock = false, int referenceType = (int)StockReferenceType.SalesOrder)
        => ExecuteWithStockRetryAsync(() => DispatchStockCoreAsync(productId, warehouseId, quantity, referenceId, userId, note, releaseReservedStock, referenceType));

    private async Task<StockMovementLogDto?> DispatchStockCoreAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note, bool releaseReservedStock, int referenceType)
    {
        if (quantity <= 0) return null;

        var product = await productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await warehouseDataReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
        if (warehouse is null)
            throw new WarehouseIsNotFoundException(warehouseId);

        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            return null;

        var availableForDispatch = releaseReservedStock ? stock.QuantityOnHand : stock.QuantityAvailable;
        if (availableForDispatch < quantity)
            throw new InsufficientStockException(productId, warehouseId, quantity, availableForDispatch);

        var quantityBefore = stock.QuantityOnHand;
        stock.QuantityOnHand -= quantity;

        if (releaseReservedStock && referenceId.HasValue)
        {
            var reservedForRef = await stockReservationDataReader.DataSource
                .Where(e => e.ProductId == productId && e.WarehouseId == warehouseId && e.ReferenceId == referenceId.Value)
                .SumAsync(e => e.QuantityDelta).ConfigureAwait(false);
            var toRelease = Math.Min(quantity, Math.Max(0, reservedForRef));
            if (toRelease > 0)
            {
                stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - toRelease);
                await stockReservationRepository.InsertAsync(new StockReservationEntry(productId, warehouseId, -toRelease, referenceId.Value, note)).ConfigureAwait(false);
            }
        }
        else if (releaseReservedStock)
        {
            stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - quantity);
        }

        stock.UpdatedOnUtc = DateTime.UtcNow;
        await inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        var log = new StockMovementLog(
            Guid.NewGuid(),
            stock.ProductId,
            stock.WarehouseId,
            StockMovementType.Outbound,
            quantity,
            quantityBefore,
            stock.QuantityOnHand,
            (StockReferenceType)referenceType,
            referenceId,
            note,
            userId
        );
        await stockMovementRepository.InsertAsync(log).ConfigureAwait(false);

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

    public async Task<StockMovementLogDto?> DispatchStockUpToAsync(Guid productId, Guid warehouseId,
        decimal targetQuantity, Guid? referenceId, Guid userId, string? note = null,
        bool releaseReservedStock = false, int referenceType = (int)StockReferenceType.SalesOrder)
    {
        if (targetQuantity <= 0)
            return null;

        var alreadyDispatched = await GetMovedQuantityAsync(
            productId,
            warehouseId,
            StockMovementType.Outbound,
            (StockReferenceType)referenceType,
            referenceId).ConfigureAwait(false);

        var missingQuantity = targetQuantity - alreadyDispatched;
        if (missingQuantity <= 0)
            return null;

        return await DispatchStockAsync(
            productId,
            warehouseId,
            missingQuantity,
            referenceId,
            userId,
            note,
            releaseReservedStock,
            referenceType).ConfigureAwait(false);
    }

    public Task<(StockMovementLogDto? OutLog, StockMovementLogDto? InLog)> TransferStockAsync(
        Guid productId, Guid fromWarehouseId, Guid toWarehouseId,
        decimal quantity, decimal unitCost,
        Guid? referenceId, Guid userId, string? note = null)
        => ExecuteWithStockRetryAsync(() => TransferStockCoreAsync(productId, fromWarehouseId, toWarehouseId, quantity, unitCost, referenceId, userId, note));

    private async Task<(StockMovementLogDto? OutLog, StockMovementLogDto? InLog)> TransferStockCoreAsync(
        Guid productId, Guid fromWarehouseId, Guid toWarehouseId,
        decimal quantity, decimal unitCost,
        Guid? referenceId, Guid userId, string? note = null)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");
        if (unitCost < 0)
            throw new InvalidStockOperationException("Error.StockAverageCostCannotBeNegative");
        if (fromWarehouseId == toWarehouseId)
            return (null, null);

        var product = await productDataReader.GetByIdAsync(productId).ConfigureAwait(false)
            ?? throw new ProductIsNotFoundException(productId);

        if (await warehouseDataReader.GetByIdAsync(fromWarehouseId).ConfigureAwait(false) is null)
            throw new WarehouseIsNotFoundException(fromWarehouseId);
        if (await warehouseDataReader.GetByIdAsync(toWarehouseId).ConfigureAwait(false) is null)
            throw new WarehouseIsNotFoundException(toWarehouseId);

        var fromStock = await TryGetInventoryStockForProductAsync(productId, fromWarehouseId).ConfigureAwait(false)
            ?? throw new StockNotFoundException("Error.StockNotFound", productId, fromWarehouseId);
        if (fromStock.QuantityAvailable < quantity)
            throw new InsufficientStockException(productId, fromWarehouseId, quantity, fromStock.QuantityAvailable);
        var fromBefore = fromStock.QuantityOnHand;
        fromStock.QuantityOnHand -= quantity;
        fromStock.UpdatedOnUtc = DateTime.UtcNow;
        await inventoryStockRepository.UpdateAsync(fromStock).ConfigureAwait(false);

        var outLog = new StockMovementLog(
            Guid.NewGuid(),
            productId,
            fromWarehouseId,
            StockMovementType.Transfer,
            quantity,
            fromBefore,
            fromStock.QuantityOnHand,
            StockReferenceType.StockTransfer,
            referenceId,
            note,
            userId);
        await stockMovementRepository.InsertAsync(outLog).ConfigureAwait(false);

        var toStock = await TryGetInventoryStockForProductAsync(productId, toWarehouseId).ConfigureAwait(false)
            ?? await EnsureInitializeStockAsync(productId, toWarehouseId, product.UnitMeasurementId).ConfigureAwait(false);
        var toBefore = toStock.QuantityOnHand;
        toStock.QuantityOnHand += quantity;
        toStock.UpdatedOnUtc = DateTime.UtcNow;
        await inventoryStockRepository.UpdateAsync(toStock).ConfigureAwait(false);

        var inLog = new StockMovementLog(
            Guid.NewGuid(),
            productId,
            toWarehouseId,
            StockMovementType.Transfer,
            quantity,
            toBefore,
            toStock.QuantityOnHand,
            StockReferenceType.StockTransfer,
            referenceId,
            note,
            userId);
        await stockMovementRepository.InsertAsync(inLog).ConfigureAwait(false);

        return (ToDto(outLog, product.Name), ToDto(inLog, product.Name));
    }

    public async Task<(int Total, List<StockMovementLogDto> Items)> GetStockMovementLogsAsync(Guid? productId, Guid? warehouseId, int pageIndex, int pageSize)
    {
        var stockMovementLogQuery = stockMovementDataReader.DataSource;

        if (warehouseId.HasValue)
            stockMovementLogQuery = stockMovementLogQuery.Where(x => x.WarehouseId == warehouseId);

        if (productId.HasValue)
            stockMovementLogQuery = stockMovementLogQuery.Where(x => x.ProductId == productId);

        var productQuery = productDataReader.DataSource;

        var query = from l in stockMovementLogQuery
                    join p in productQuery on l.ProductId equals p.Id
                    select new { l, p };

        var total = await query.CountAsync().ConfigureAwait(false);

        var items = await query
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
            .ToListAsync().ConfigureAwait(false);

        return (total, items);
    }

    public async Task<(StockMovementLogDto? OutLog, StockMovementLogDto? InLog)> TransferStockUpToAsync(
        Guid productId, Guid fromWarehouseId, Guid toWarehouseId,
        decimal targetQuantity, decimal unitCost,
        Guid? referenceId, Guid userId, string? note = null)
    {
        if (targetQuantity <= 0)
            return (null, null);

        var alreadyTransferred = await GetMovedQuantityAsync(productId, fromWarehouseId, StockMovementType.Transfer, StockReferenceType.StockTransfer, referenceId).ConfigureAwait(false);

        var remaining = targetQuantity - alreadyTransferred;
        if (remaining <= 0)
            return (null, null);

        return await TransferStockAsync(productId, fromWarehouseId, toWarehouseId, remaining, unitCost, referenceId, userId, note).ConfigureAwait(false);
    }

    public (bool IsLowStock, decimal ReorderLevel) IsLowStock(InventoryStock stock)
    {
        if (stock.ReorderLevel <= 0)
            return (false, stock.ReorderLevel); // No reorder level set

        return (stock.QuantityOnHand < stock.ReorderLevel, stock.ReorderLevel);
    }

    public (bool IsOverstocked, decimal MaxLevel) IsOverstocked(InventoryStock stock)
    {
        if (stock.MaxStockLevel <= 0)
            return (false, stock.MaxStockLevel); // No max level set

        return (stock.QuantityOnHand > stock.MaxStockLevel, stock.MaxStockLevel);
    }

    public Task ApplyAdjustmentAsync(Guid productId, Guid warehouseId, decimal delta, Guid adjustmentNoteId, Guid? userId)
        => ExecuteWithStockRetryAsync(() => ApplyAdjustmentCoreAsync(productId, warehouseId, delta, adjustmentNoteId, userId));

    private async Task ApplyAdjustmentCoreAsync(Guid productId, Guid warehouseId, decimal delta, Guid adjustmentNoteId, Guid? userId)
    {
        if (delta == 0) return;

        var product = await productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null) throw new ProductIsNotFoundException(productId);

        var warehouse = await warehouseDataReader.GetByIdAsync(warehouseId).ConfigureAwait(false);
        if (warehouse is null) throw new WarehouseIsNotFoundException(warehouseId);

        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            stock = await EnsureInitializeStockAsync(productId, warehouseId, product.UnitMeasurementId).ConfigureAwait(false);

        if (delta < 0 && Math.Abs(delta) > stock.QuantityAvailable)
            throw new InsufficientStockException(productId, warehouseId, Math.Abs(delta), stock.QuantityAvailable);

        var before = stock.QuantityOnHand;
        stock.QuantityOnHand += delta;
        stock.UpdatedOnUtc = DateTime.UtcNow;
        await inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        var log = new StockMovementLog(Guid.NewGuid(), productId, warehouseId,
            StockMovementType.Adjustment, Math.Abs(delta), before, stock.QuantityOnHand,
            StockReferenceType.Adjustment, adjustmentNoteId,
            delta > 0 ? "Điều chỉnh tăng tồn kho" : "Điều chỉnh giảm tồn kho", userId);
        await stockMovementRepository.InsertAsync(log).ConfigureAwait(false);
    }

    private async Task<InventoryStock?> TryGetInventoryStockForProductAsync(Guid productId, Guid warehouseId)
    {
        var stock = await inventoryStockDataReader.GetDataSource(new() { ReadWrite = true })
            .Where(inventoryStock => inventoryStock.ProductId == productId && inventoryStock.WarehouseId == warehouseId)
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);
        return stock;
    }

    private Task<decimal> GetMovedQuantityAsync(Guid productId, Guid warehouseId,
        StockMovementType movementType, StockReferenceType referenceType, Guid? referenceId)
    {
        return stockMovementDataReader.GetDataSource(new() { ReadWrite = true })
                .Where(log => log.ProductId == productId
                    && log.WarehouseId == warehouseId
                    && log.MovementType == movementType
                    && log.ReferenceType == referenceType
                    && log.ReferenceId == referenceId)
                .SumAsync(log => log.Quantity);
    }

    private static StockMovementLogDto ToDto(StockMovementLog log, string productName)
    {
        return new(log.Id)
        {
            ProductId = log.ProductId,
            ProductName = productName,
            MovementType = (int)log.MovementType,
            Quantity = log.Quantity,
            QuantityBefore = log.QuantityBefore,
            QuantityAfter = log.QuantityAfter,
            CreatedOnUtc = log.CreatedOnUtc,
            Note = log.Note
        };
    }

    public Task SetStockLevelsAsync(SetStockLevelsDto dto)
        => ExecuteWithStockRetryAsync(() => SetStockLevelsCoreAsync(dto));

    private async Task SetStockLevelsCoreAsync(SetStockLevelsDto dto)
    {
        dto.Verify();

        var stock = await inventoryStockRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (stock is null)
            throw new StockNotFoundException("Error.StockNotFound", dto.Id);

        stock.ReorderLevel = dto.ReorderLevel;
        stock.MaxStockLevel = dto.MaxStockLevel;
        stock.UpdatedOnUtc = DateTime.UtcNow;

        await inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);
    }

    public async Task<InventoryStockDto?> GetInventoryStockForProductAsync(Guid productId, Guid warehouseId)
    {
        var stocks = await GetInventoryStocksAsync(0, 1, warehouseId: warehouseId, productId: productId);

        return stocks.Items.FirstOrDefault();
    }

    private async Task ExecuteWithStockRetryAsync(Func<Task> action, int maxRetries = 3)
    {
        var attempts = 0;
        while (true)
        {
            try { await action().ConfigureAwait(false); return; }
            catch (DbUpdateConcurrencyException ex) when (++attempts < maxRetries)
            {
                foreach (var entry in ex.Entries)
                    await entry.ReloadAsync().ConfigureAwait(false);
            }
        }
    }

    private async Task<T> ExecuteWithStockRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3)
    {
        var attempts = 0;
        while (true)
        {
            try { return await action().ConfigureAwait(false); }
            catch (DbUpdateConcurrencyException ex) when (++attempts < maxRetries)
            {
                foreach (var entry in ex.Entries)
                    await entry.ReloadAsync().ConfigureAwait(false);
            }
        }
    }

}
