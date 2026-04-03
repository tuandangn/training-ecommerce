using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Shared.Services.Inventory;

/// <summary>
/// #10 Audit Logging Service: Logs all stock operations for compliance, debugging, and tracing
/// Includes operation type, before/after values, user tracking, and system trace ID for cross-service tracing
/// </summary>
public interface IStockAuditLogger
{
    /// <summary>
    /// Log a stock operation with full audit trail
    /// </summary>
    Task LogStockOperationAsync(
        Guid productId,
        string productName,
        Guid warehouseId,
        string warehouseName,
        string operationType, // Reserve, Release, Dispatch, Receive, Adjust
        decimal quantity,
        decimal oldValue,
        decimal newValue,
        Guid? performedByUserId,
        string? performedByUserName,
        string? note,
        string? traceId);

    /// <summary>
    /// Get audit logs for a specific product
    /// </summary>
    Task<List<StockAuditLogDto>> GetAuditLogsAsync(Guid productId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get audit logs for a specific warehouse
    /// </summary>
    Task<List<StockAuditLogDto>> GetWarehouseAuditLogsAsync(Guid warehouseId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get recent operations (for dashboard, compliance reports)
    /// </summary>
    Task<List<StockAuditLogDto>> GetRecentOperationsAsync(int limit = 100);
}
