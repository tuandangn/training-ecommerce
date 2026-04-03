namespace NamEcommerce.Domain.Shared.Dtos.Inventory;

/// <summary>
/// #10 Audit Logging DTO: Captures all stock operation changes for compliance and tracing
/// </summary>
public class StockAuditLogDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; }
    
    /// <summary>Operation type: Reserve, Release, Dispatch, Receive, Adjust</summary>
    public string OperationType { get; set; }
    
    /// <summary>Quantity changed</summary>
    public decimal Quantity { get; set; }
    
    /// <summary>Previous value</summary>
    public decimal OldValue { get; set; }
    
    /// <summary>New value after operation</summary>
    public decimal NewValue { get; set; }
    
    /// <summary>User who performed operation</summary>
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByUserName { get; set; }
    
    /// <summary>Reason/Note for operation</summary>
    public string? Note { get; set; }
    
    /// <summary>System trace ID for tracing across services</summary>
    public string? TraceId { get; set; }
    
    /// <summary>Timestamp when operation occurred</summary>
    public DateTime CreatedOnUtc { get; set; }
}
