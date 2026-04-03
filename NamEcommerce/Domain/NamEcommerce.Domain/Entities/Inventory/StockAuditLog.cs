using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Inventory;

/// <summary>
/// #10 Audit Logging Entity: Complete history of all stock operations for compliance
/// Tracks: operation type, before/after values, user, timestamp, and system trace ID
/// </summary>
[Serializable]
public sealed record StockAuditLog : AppAggregateEntity
{
    internal StockAuditLog(
        Guid id,
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
        string? traceId) : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        WarehouseId = warehouseId;
        WarehouseName = warehouseName;
        OperationType = operationType;
        Quantity = quantity;
        OldValue = oldValue;
        NewValue = newValue;
        PerformedByUserId = performedByUserId;
        PerformedByUserName = performedByUserName;
        Note = note;
        TraceId = traceId ?? Guid.NewGuid().ToString();
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; init; }
    public string ProductName { get; init; }
    
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; }
    
    /// <summary>Operation type: Reserve, Release, Dispatch, Receive, Adjust</summary>
    public string OperationType { get; init; }
    
    /// <summary>Quantity changed in this operation</summary>
    public decimal Quantity { get; init; }
    
    /// <summary>Previous quantity value before operation</summary>
    public decimal OldValue { get; init; }
    
    /// <summary>New quantity value after operation</summary>
    public decimal NewValue { get; init; }
    
    /// <summary>User ID who performed this operation</summary>
    public Guid? PerformedByUserId { get; init; }
    
    /// <summary>Username for easier reporting</summary>
    public string? PerformedByUserName { get; init; }
    
    /// <summary>Reason/note for the operation</summary>
    public string? Note { get; init; }
    
    /// <summary>System trace ID for tracing operation across microservices</summary>
    public string TraceId { get; init; }
    
    /// <summary>When this operation occurred</summary>
    public DateTime CreatedOnUtc { get; init; }

    /// <summary>
    /// Factory method to create audit log
    /// </summary>
    internal static StockAuditLog Create(
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
        string? traceId) =>
        new(
            Guid.NewGuid(),
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
}
