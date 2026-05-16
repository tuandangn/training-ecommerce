namespace NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

public enum AllocationStatus
{
    /// <summary>Đã phân bổ, chưa nhận hàng.</summary>
    Allocated = 0,
    /// <summary>Đã nhận một phần.</summary>
    PartiallyReceived = 1,
    /// <summary>Đã nhận đủ — đang ở kho (kho thường hoặc kho ảo transit).</summary>
    FullyReceived = 2,
    /// <summary>Hàng đang ở kho ảo Direct-Ship Transit, chờ xác nhận khách nhận.</summary>
    DeliveryPending = 3,
    /// <summary>Khách đã xác nhận nhận hàng.</summary>
    DeliveryConfirmed = 4,
    /// <summary>Bị huỷ.</summary>
    Cancelled = 5
}
