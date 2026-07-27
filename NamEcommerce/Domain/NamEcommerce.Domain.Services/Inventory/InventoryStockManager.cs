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
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class InventoryStockManager : IInventoryStockManager
{
    private readonly IRepository<InventoryStock> _inventoryStockRepository;
    private readonly IRepository<StockMovementLog> _stockMovementRepository;
    private readonly IRepository<StockReservationEntry> _stockReservationRepository;
    private readonly IEntityDataReader<StockMovementLog> _stockMovementDataReader;
    private readonly IEntityDataReader<InventoryStock> _inventoryStockDataReader;
    private readonly IEntityDataReader<ProductReservationLedger> _productReservationDataReader;
    private readonly IEntityDataReader<StockReservationEntry> _stockReservationDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;

    public InventoryStockManager(IRepository<InventoryStock> inventoryStockRepository, IEntityDataReader<InventoryStock> inventoryStockDataReader, IStockAuditLogger stockAuditLogger, IRepository<StockMovementLog> stockMovementRepository, IRepository<StockReservationEntry> stockReservationRepository, IEntityDataReader<Product> productDataReader, IEntityDataReader<Warehouse> warehouseDataReader, IEntityDataReader<StockMovementLog> stockMovementDataReader, IEntityDataReader<ProductReservationLedger> productReservationDataReader, IEntityDataReader<StockReservationEntry> stockReservationDataReader)
    {
        _inventoryStockRepository = inventoryStockRepository;
        _inventoryStockDataReader = inventoryStockDataReader;
        _productReservationDataReader = productReservationDataReader;
        _stockReservationRepository = stockReservationRepository;
        _stockReservationDataReader = stockReservationDataReader;
        _stockMovementRepository = stockMovementRepository;
        _productDataReader = productDataReader;
        _warehouseDataReader = warehouseDataReader;
        _stockMovementDataReader = stockMovementDataReader;
    }

    public async Task InitializeStockAsync(Guid productId, Guid warehouseId, Guid? unitMeasurementId = null)
        => await EnsureInitializeStockAsync(productId, warehouseId, unitMeasurementId).ConfigureAwait(false);

    private readonly IDictionary<IdPair, InventoryStock> _cachedInventoryStocks = new Dictionary<IdPair, InventoryStock>();
    private async Task<InventoryStock> EnsureInitializeStockAsync(Guid productId, Guid warehouseId, Guid? unitMeasurementId = null)
    {
        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is not null)
            return stock;

        stock = new InventoryStock(Guid.NewGuid(), productId, warehouseId, unitMeasurementId);
        await _inventoryStockRepository.InsertAsync(stock).ConfigureAwait(false);
        _cachedInventoryStocks.Add((productId, warehouseId), stock);

        return stock;
    }

    public Task<StockMovementLogDto?> ReceiveStockAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId, bool enforceCapacity = true)
        => ExecuteWithStockRetryAsync(() => ReceiveCoreAsync(productId, warehouseId, receivedQuantity, note, receivedByUserId, referenceType, referenceId, enforceCapacity));

    private async Task<StockMovementLogDto?> ReceiveCoreAsync(Guid productId, Guid warehouseId, decimal receivedQuantity, string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId, bool enforceCapacity = true)
    {
        if (receivedQuantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var product = await _productDataReader.GetByIdAsync(productId, default).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId, default).ConfigureAwait(false);
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

    public Task<StockMovementLogDto?> ReceiveStockUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity,
        string? note, Guid? receivedByUserId, int referenceType, Guid? referenceId, bool enforceCapacity = true)
    {
        if (targetQuantity <= 0)
            return Task.FromResult<StockMovementLogDto?>(null);

        var alreadyReceived = GetMovedQuantity(
            productId,
            warehouseId,
            StockMovementType.Inbound,
            (StockReferenceType)referenceType,
            referenceId);
        var missingQuantity = targetQuantity - alreadyReceived;
        return missingQuantity <= 0
            ? Task.FromResult<StockMovementLogDto?>(null)
            : ReceiveStockAsync(productId, warehouseId, missingQuantity, note, receivedByUserId, referenceType, referenceId, enforceCapacity);
    }

    public Task<StockMovementLogDto?> RevertReceiveAsync(Guid productId, Guid warehouseId, decimal quantity, Guid goodsReceiptId, Guid modifiedByUserId)
        => ExecuteWithStockRetryAsync(() => RevertReceiveCoreAsync(productId, warehouseId, quantity, goodsReceiptId, modifiedByUserId));

    private async Task<StockMovementLogDto?> RevertReceiveCoreAsync(Guid productId, Guid warehouseId, decimal quantity, Guid goodsReceiptId, Guid modifiedByUserId)
    {
        if (quantity <= 0)
            throw new InvalidStockOperationException("Error.StockQuantityMustBePositive");

        var product = await _productDataReader.GetByIdAsync(productId, default).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId, default).ConfigureAwait(false);
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
        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

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
        await _stockMovementRepository.InsertAsync(stockMovementLog).ConfigureAwait(false);

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

    public Task<StockMovementLogDto?> RevertReceiveUpToAsync(Guid productId, Guid warehouseId, decimal targetQuantity, Guid goodsReceiptId, Guid modifiedByUserId)
    {
        if (targetQuantity <= 0)
            return Task.FromResult<StockMovementLogDto?>(null);

        var alreadyReverted = GetMovedQuantity(
            productId,
            warehouseId,
            StockMovementType.Revert,
            StockReferenceType.GoodsReceipt,
            goodsReceiptId);
        var missingQuantity = targetQuantity - alreadyReverted;
        return missingQuantity <= 0
            ? Task.FromResult<StockMovementLogDto?>(null)
            : RevertReceiveAsync(productId, warehouseId, missingQuantity, goodsReceiptId, modifiedByUserId);
    }

    public Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(int pageIndex, int pageSize,
        Guid? warehouseId = null, Guid? productId = null, string? keywords = null)
        => GetInventoryStocksAsync(pageIndex, pageSize, [warehouseId], [productId], keywords: keywords);

    public async Task<(int Total, List<InventoryStockDto> Items)> GetInventoryStocksAsync(int pageIndex, int pageSize, Guid?[]? warehouseIds = null, Guid?[]? productIds = null, string? keywords = null)
    {
        var inventoryStockQuery = _inventoryStockDataReader.DataSource;
        var searchProductIds = productIds?.OfType<Guid>().ToArray() ?? [];
        var searchWarehouseIds = warehouseIds?.OfType<Guid>().ToArray() ?? [];
        if (searchProductIds.Length > 0)
            inventoryStockQuery = inventoryStockQuery.Where(x => searchProductIds.Contains(x.ProductId));
        if (searchWarehouseIds.Length > 0)
            inventoryStockQuery = inventoryStockQuery.Where(x => searchWarehouseIds.Contains(x.WarehouseId));

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
        var inventoryStockQuery = _inventoryStockDataReader.DataSource;
        inventoryStockQuery = inventoryStockQuery.Where(x => x.ProductId == productId);

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
                UpdatedOnUtc = x.s.UpdatedOnUtc,
                ReorderLevel = x.s.ReorderLevel,
                MaxStockLevel = x.s.MaxStockLevel
            })
            .OrderByDescending(x => x.UpdatedOnUtc)
            .ThenBy(x => x.ProductName)
            .ToList();

        return items;
    }

    public Task<decimal> GetGlobalOnHandQuantityForProductAsync(Guid productId)
    {
        var stockQuery = from s in _inventoryStockDataReader.DataSource
                         join w in _warehouseDataReader.DataSource on s.WarehouseId equals w.Id
                         where s.ProductId == productId && w.WarehouseType == WarehouseType.Physical
                         select s;
        return Task.FromResult(stockQuery.Sum(x => x.QuantityOnHand));
    }

    public Task<decimal> GetGlobalAvailableQuantityForProductAsync(Guid productId, Guid? excludeOrderId = null)
    {
        var stockQuery = from s in _inventoryStockDataReader.DataSource
                         join w in _warehouseDataReader.DataSource on s.WarehouseId equals w.Id
                         where s.ProductId == productId && w.WarehouseType == WarehouseType.Physical
                         select s;
        var quantityOnHand = stockQuery.Sum(x => x.QuantityOnHand);
        var quantityReservedByWarehouse = stockQuery.Sum(x => x.QuantityReserved);
        var quantityReservedByOrder = _productReservationDataReader.DataSource
            .Where(x => x.ProductId == productId && (excludeOrderId == null || x.OrderId != excludeOrderId))
            .Sum(x => x.QuantityDelta);

        return Task.FromResult(quantityOnHand - quantityReservedByWarehouse - quantityReservedByOrder);
    }

    public async Task<decimal> ComputeAvailableQuantityForOrderAsync(Guid productId, Guid orderId)
    {
        var globalAvailable = await GetGlobalAvailableQuantityForProductAsync(productId).ConfigureAwait(false);
        var reservedForOrder = _productReservationDataReader.DataSource
            .Where(x => x.ProductId == productId && x.OrderId == orderId)
            .Sum(x => x.QuantityDelta);

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

        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        if (referenceId.HasValue)
            await _stockReservationRepository.InsertAsync(new StockReservationEntry(productId, warehouseId, quantity, referenceId.Value, note)).ConfigureAwait(false);

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
            ? _stockReservationDataReader.DataSource
                .Where(e => e.ProductId == productId && e.WarehouseId == warehouseId && e.ReferenceId == referenceId.Value)
                .Sum(e => e.QuantityDelta)
            : quantity;

        var toRelease = Math.Min(quantity, Math.Max(0, reservedForRef));
        if (toRelease <= 0)
            return true;

        stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - toRelease);
        stock.UpdatedOnUtc = DateTime.UtcNow;
        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        if (referenceId.HasValue)
            await _stockReservationRepository.InsertAsync(new StockReservationEntry(productId, warehouseId, -toRelease, referenceId.Value, note)).ConfigureAwait(false);

        return true;
    }

    public Task<StockMovementLogDto?> DispatchStockAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note = null, bool releaseReservedStock = false, int referenceType = (int)StockReferenceType.SalesOrder)
        => ExecuteWithStockRetryAsync(() => DispatchStockCoreAsync(productId, warehouseId, quantity, referenceId, userId, note, releaseReservedStock, referenceType));

    private async Task<StockMovementLogDto?> DispatchStockCoreAsync(Guid productId, Guid warehouseId, decimal quantity, Guid? referenceId, Guid userId, string? note, bool releaseReservedStock, int referenceType)
    {
        if (quantity <= 0) return null;

        var product = await _productDataReader.GetByIdAsync(productId, default).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId, default).ConfigureAwait(false);
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
            var reservedForRef = _stockReservationDataReader.DataSource
                .Where(e => e.ProductId == productId && e.WarehouseId == warehouseId && e.ReferenceId == referenceId.Value)
                .Sum(e => e.QuantityDelta);
            var toRelease = Math.Min(quantity, Math.Max(0, reservedForRef));
            if (toRelease > 0)
            {
                stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - toRelease);
                await _stockReservationRepository.InsertAsync(new StockReservationEntry(productId, warehouseId, -toRelease, referenceId.Value, note)).ConfigureAwait(false);
            }
        }
        else if (releaseReservedStock)
        {
            stock.QuantityReserved = Math.Max(0, stock.QuantityReserved - quantity);
        }

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
            (StockReferenceType)referenceType,
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

    public Task<StockMovementLogDto?> DispatchStockUpToAsync(Guid productId, Guid warehouseId,
        decimal targetQuantity, Guid? referenceId, Guid userId, string? note = null,
        bool releaseReservedStock = false, int referenceType = (int)StockReferenceType.SalesOrder)
    {
        if (targetQuantity <= 0)
            return Task.FromResult<StockMovementLogDto?>(null);

        var alreadyDispatched = GetMovedQuantity(
            productId,
            warehouseId,
            StockMovementType.Outbound,
            (StockReferenceType)referenceType,
            referenceId);
        var missingQuantity = targetQuantity - alreadyDispatched;
        return missingQuantity <= 0
            ? Task.FromResult<StockMovementLogDto?>(null)
            : DispatchStockAsync(
                productId,
                warehouseId,
                missingQuantity,
                referenceId,
                userId,
                note,
                releaseReservedStock,
                referenceType);
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

        var product = await _productDataReader.GetByIdAsync(productId, default).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        var fromWarehouse = await _warehouseDataReader.GetByIdAsync(fromWarehouseId, default).ConfigureAwait(false);
        if (fromWarehouse is null)
            throw new WarehouseIsNotFoundException(fromWarehouseId);

        var toWarehouse = await _warehouseDataReader.GetByIdAsync(toWarehouseId, default).ConfigureAwait(false);
        if (toWarehouse is null)
            throw new WarehouseIsNotFoundException(toWarehouseId);

        var fromStock = await TryGetInventoryStockForProductAsync(productId, fromWarehouseId).ConfigureAwait(false);
        if (fromStock is null)
            throw new StockNotFoundException("Error.StockNotFound", productId, fromWarehouseId);
        if (fromStock.QuantityAvailable < quantity)
            throw new InsufficientStockException(productId, fromWarehouseId, quantity, fromStock.QuantityAvailable);

        var toStock = await TryGetInventoryStockForProductAsync(productId, toWarehouseId).ConfigureAwait(false);
        if (toStock is null)
            toStock = await EnsureInitializeStockAsync(productId, toWarehouseId, product.UnitMeasurementId).ConfigureAwait(false);

        var fromBefore = fromStock.QuantityOnHand;
        var toBefore = toStock.QuantityOnHand;

        fromStock.QuantityOnHand -= quantity;
        fromStock.UpdatedOnUtc = DateTime.UtcNow;

        toStock.QuantityOnHand += quantity;

        toStock.UpdatedOnUtc = DateTime.UtcNow;

        await _inventoryStockRepository.UpdateAsync(fromStock).ConfigureAwait(false);
        await _inventoryStockRepository.UpdateAsync(toStock).ConfigureAwait(false);

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

        await _stockMovementRepository.InsertAsync(outLog).ConfigureAwait(false);
        await _stockMovementRepository.InsertAsync(inLog).ConfigureAwait(false);

        return (ToDto(outLog, product.Name), ToDto(inLog, product.Name));
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

    [Obsolete("Expiry-based reservation release is replaced by per-reference ledger (StockReservationEntry). This method is a no-op.")]
    public Task<int> ReleaseExpiredReservationsAsync() => Task.FromResult(0);

    public Task<(StockMovementLogDto? OutLog, StockMovementLogDto? InLog)> TransferStockUpToAsync(
        Guid productId, Guid fromWarehouseId, Guid toWarehouseId,
        decimal targetQuantity, decimal unitCost,
        Guid? referenceId, Guid userId, string? note = null)
    {
        if (targetQuantity <= 0)
            return Task.FromResult<(StockMovementLogDto?, StockMovementLogDto?)>((null, null));

        var alreadyTransferred = GetMovedQuantity(productId, fromWarehouseId, StockMovementType.Transfer, StockReferenceType.StockTransfer, referenceId);
        var remaining = targetQuantity - alreadyTransferred;
        return remaining <= 0
            ? Task.FromResult<(StockMovementLogDto?, StockMovementLogDto?)>((null, null))
            : TransferStockAsync(productId, fromWarehouseId, toWarehouseId, remaining, unitCost, referenceId, userId, note);
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

        var product = await _productDataReader.GetByIdAsync(productId, default).ConfigureAwait(false);
        if (product is null) throw new ProductIsNotFoundException(productId);

        var warehouse = await _warehouseDataReader.GetByIdAsync(warehouseId, default).ConfigureAwait(false);
        if (warehouse is null) throw new WarehouseIsNotFoundException(warehouseId);

        var stock = await TryGetInventoryStockForProductAsync(productId, warehouseId).ConfigureAwait(false);
        if (stock is null)
            stock = await EnsureInitializeStockAsync(productId, warehouseId, product.UnitMeasurementId).ConfigureAwait(false);

        if (delta < 0 && Math.Abs(delta) > stock.QuantityAvailable)
            throw new InsufficientStockException(productId, warehouseId, Math.Abs(delta), stock.QuantityAvailable);

        var before = stock.QuantityOnHand;
        stock.QuantityOnHand += delta;
        stock.UpdatedOnUtc = DateTime.UtcNow;
        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);

        var log = new StockMovementLog(Guid.NewGuid(), productId, warehouseId,
            StockMovementType.Adjustment, Math.Abs(delta), before, stock.QuantityOnHand,
            StockReferenceType.Adjustment, adjustmentNoteId,
            delta > 0 ? "Điều chỉnh tăng tồn kho" : "Điều chỉnh giảm tồn kho", userId);
        await _stockMovementRepository.InsertAsync(log).ConfigureAwait(false);
    }

    private async Task<InventoryStock?> TryGetInventoryStockForProductAsync(Guid productId, Guid warehouseId)
    {
        InventoryStock? stock = null;
        if (_cachedInventoryStocks.TryGetValue((productId, warehouseId), out stock))
            return stock;

        var stockId = await _inventoryStockDataReader.DataSource
            .Where(inventoryStock => inventoryStock.ProductId == productId && inventoryStock.WarehouseId == warehouseId)
            .Select(inventoryStock => inventoryStock.Id)
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);
        if (stockId == Guid.Empty)
            return null;

        stock = await _inventoryStockRepository.GetByIdAsync(stockId).ConfigureAwait(false);
        if (stock is not null)
            _cachedInventoryStocks.Add((productId, warehouseId), stock);

        return stock;
    }

    private decimal GetMovedQuantity(Guid productId, Guid warehouseId,
        StockMovementType movementType, StockReferenceType referenceType, Guid? referenceId)
        => _stockMovementDataReader.DataSource
            .Where(log => log.ProductId == productId
                && log.WarehouseId == warehouseId
                && log.MovementType == movementType
                && log.ReferenceType == referenceType
                && log.ReferenceId == referenceId)
            .Sum(log => log.Quantity);

    private static StockMovementLogDto ToDto(StockMovementLog log, string productName)
        => new(log.Id)
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

    public Task SetStockLevelsAsync(SetStockLevelsDto dto)
        => ExecuteWithStockRetryAsync(() => SetStockLevelsCoreAsync(dto));

    private async Task SetStockLevelsCoreAsync(SetStockLevelsDto dto)
    {
        dto.Verify();

        var stock = await _inventoryStockRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (stock is null)
            throw new StockNotFoundException("Error.StockNotFound", dto.Id);

        stock.ReorderLevel = dto.ReorderLevel;
        stock.MaxStockLevel = dto.MaxStockLevel;
        stock.UpdatedOnUtc = DateTime.UtcNow;

        await _inventoryStockRepository.UpdateAsync(stock).ConfigureAwait(false);
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
                _cachedInventoryStocks.Clear();
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
                _cachedInventoryStocks.Clear();
            }
        }
    }

}
