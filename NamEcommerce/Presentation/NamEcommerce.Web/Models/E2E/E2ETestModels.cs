namespace NamEcommerce.Web.Models.E2E;

public sealed record E2EResetRequest(string? ScenarioId);

public sealed record E2ESeedOrderWorkflowRequest
{
    public required string ScenarioId { get; init; }
    public required decimal Quantity { get; init; }
    public required bool DirectShip { get; init; }
}

public sealed record E2ESeedOrderWorkflowResult
{
    public required string ScenarioId { get; init; }
    public required decimal Quantity { get; init; }
    public required string CustomerName { get; init; }
    public required string CustomerPhone { get; init; }
    public required string ShippingAddress { get; init; }
    public required string VendorName { get; init; }
    public required string WarehouseName { get; init; }
    public required string ProductName { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal UnitCost { get; init; }
}

public sealed record E2EOrderWorkflowState
{
    public required string ScenarioId { get; init; }
    public string? OrderCode { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public required string OrderStatus { get; init; }
    public required string PurchaseOrderStatus { get; init; }
    public required string DeliveryStatus { get; init; }
    public required decimal OrderedQuantity { get; init; }
    public required decimal ReceivedQuantity { get; init; }
    public required decimal DeliveredQuantity { get; init; }

    public required E2EInventoryStockState StockInfo { get; set; }
}

public sealed record E2EInventoryStockState
{
    public required string ScenarioId { get; init; }
    public required decimal StockOnHandQuantity { get; init; }
    public required decimal StockReservedQuantity { get; init; }
    public required decimal StockAvailableQuantity { get; init; }
    public required decimal GlobalReservedQuantity { get; init; }
}

