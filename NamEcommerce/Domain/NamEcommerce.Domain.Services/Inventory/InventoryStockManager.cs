using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class InventoryStockManager : IInventoryStockManager
{
    private readonly IDbContext _dbContext;

    public InventoryStockManager(IDbContext dbContext)
    {
        _dbContext = dbContext;
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
            StockReferenceType.Adjustment,
            null,
            note,
            modifiedByUserId
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
            query = query.Where(x => x.p.Name.Contains(keywords) || x.w.Name.Contains(keywords));
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
}
