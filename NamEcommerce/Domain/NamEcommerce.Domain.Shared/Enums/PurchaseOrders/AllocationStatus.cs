namespace NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

public enum AllocationStatus
{
    Allocated = 0,
    PartiallyReceived = 1,
    FullyReceived = 2,
    DeliveryPending = 3,
    DeliveryConfirmed = 4,
    Cancelled = 5
}
