namespace NamEcommerce.Domain.Shared.Enums.DeliveryNotes;

public enum DeliverySettlementApprovalStatus
{
    /// <summary>Giao bình thường — thu đủ, không trả hàng. Không cần admin duyệt.</summary>
    NotRequired = 0,
    /// <summary>Shipper đã gửi duyệt thu hụt — chờ admin.</summary>
    PendingApproval = 1,
    /// <summary>Admin đã duyệt số tiền thu — shipper được thu &amp; xác nhận đã giao.</summary>
    Approved = 2,
    /// <summary>Admin từ chối — hàng mang về, phiếu hủy.</summary>
    Rejected = 3
}
