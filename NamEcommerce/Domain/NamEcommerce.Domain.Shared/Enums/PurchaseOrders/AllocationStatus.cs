namespace NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

public enum AllocationStatus
{
    Allocated = 0,
    PartiallyReceived = 1,
    FullyReceived = 2,
    /// <summary>Hàng đang ở kho ảo Direct-Ship Transit, chờ xác nhận khách nhận.</summary>
    DeliveryPending = 3,
    DeliveryConfirmed = 4,
    Cancelled = 5
}
