namespace NamEcommerce.Domain.Shared.Enums.Inventory;

public enum InventoryCostingMethod
{
    WeightedAverage = 0,
    FIFO = 1,
    LIFO = 2
}

public enum InventoryValuationScope
{
    Product = 0,
    ProductWarehouse = 1
}

public enum InventoryCostMovementType
{
    GoodsReceipt = 0,
    SaleDispatch = 1,
    CustomerReturn = 2,
    VendorReturn = 3,
    TransferOut = 4,
    TransferIn = 5,
    PositiveAdjustment = 6,
    NegativeAdjustment = 7,
    Revaluation = 8,
    RevertReceipt = 9,
    VendorReturnReversal = 10
}

public enum InventoryCostingStatus
{
    Pending = 0,
    Final = 1,
    Revalued = 2,
    Superseded = 3
}

public enum InventoryCostRebuildStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum InventoryCostRebuildTrigger
{
    ReceiptCostAssigned = 0,
    PolicyRebuild = 1,
    ManualRepair = 2
}

public enum InventoryCostReferenceType
{
    None = 0,
    PurchaseOrder = 1,
    SalesOrder = 2,
    StockIssue = 3,
    StockTransfer = 4,
    Adjustment = 5,
    GoodsReceipt = 6,
    CustomerReturn = 7,
    VendorReturn = 8
}
