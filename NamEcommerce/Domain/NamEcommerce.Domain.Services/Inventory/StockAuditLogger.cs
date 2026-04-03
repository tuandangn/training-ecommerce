using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

/// <summary>
/// #10 Audit Logger Implementation: Records all stock operations for compliance and debugging
/// </summary>
public sealed class StockAuditLogger : IStockAuditLogger
{
    private readonly IDbContext _dbContext;

    public StockAuditLogger(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogStockOperationAsync(
        Guid productId,
        string productName,
        Guid warehouseId,
        string warehouseName,
        string operationType,
        decimal quantity,
        decimal oldValue,
        decimal newValue,
        Guid? performedByUserId,
        string? performedByUserName,
        string? note,
        string? traceId)
    {
        var auditLog = StockAuditLog.Create(
            productId,
            productName,
            warehouseId,
            warehouseName,
            operationType,
            quantity,
            oldValue,
            newValue,
            performedByUserId,
            performedByUserName,
            note,
            traceId);

        await _dbContext.AddAsync(auditLog, CancellationToken.None);
    }

    public async Task<List<StockAuditLogDto>> GetAuditLogsAsync(
        Guid productId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var logs = await _dbContext.GetDataAsync<StockAuditLog>();
        
        var filtered = logs.Where(l => l.ProductId == productId);

        if (startDate.HasValue)
            filtered = filtered.Where(l => l.CreatedOnUtc >= startDate.Value);

        if (endDate.HasValue)
            filtered = filtered.Where(l => l.CreatedOnUtc <= endDate.Value);

        return filtered
            .OrderByDescending(l => l.CreatedOnUtc)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<StockAuditLogDto>> GetWarehouseAuditLogsAsync(
        Guid warehouseId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var logs = await _dbContext.GetDataAsync<StockAuditLog>();
        
        var filtered = logs.Where(l => l.WarehouseId == warehouseId);

        if (startDate.HasValue)
            filtered = filtered.Where(l => l.CreatedOnUtc >= startDate.Value);

        if (endDate.HasValue)
            filtered = filtered.Where(l => l.CreatedOnUtc <= endDate.Value);

        return filtered
            .OrderByDescending(l => l.CreatedOnUtc)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<List<StockAuditLogDto>> GetRecentOperationsAsync(int limit = 100)
    {
        var logs = await _dbContext.GetDataAsync<StockAuditLog>();
        
        return logs
            .OrderByDescending(l => l.CreatedOnUtc)
            .Take(limit)
            .Select(MapToDto)
            .ToList();
    }

    private static StockAuditLogDto MapToDto(StockAuditLog log) =>
        new()
        {
            Id = log.Id,
            ProductId = log.ProductId,
            ProductName = log.ProductName,
            WarehouseId = log.WarehouseId,
            WarehouseName = log.WarehouseName,
            OperationType = log.OperationType,
            Quantity = log.Quantity,
            OldValue = log.OldValue,
            NewValue = log.NewValue,
            PerformedByUserId = log.PerformedByUserId,
            PerformedByUserName = log.PerformedByUserName,
            Note = log.Note,
            TraceId = log.TraceId,
            CreatedOnUtc = log.CreatedOnUtc
        };
}
