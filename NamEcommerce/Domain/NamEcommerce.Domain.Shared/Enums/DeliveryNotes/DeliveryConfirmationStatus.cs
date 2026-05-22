namespace NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

public enum DeliveryConfirmationStatus
{
    /// <summary>Phiếu xuất thường (không phải direct-ship). Default cho DN cũ sau migration.</summary>
    NotApplicable = 0,
    /// <summary>Direct-ship — chờ khách xác nhận đã nhận.</summary>
    PendingConfirmation = 1,
    /// <summary>Khách đã xác nhận nhận hàng.</summary>
    Confirmed = 2,
    /// <summary>Khách từ chối / hàng chuyển về kho chính.</summary>
    Rejected = 3
}
