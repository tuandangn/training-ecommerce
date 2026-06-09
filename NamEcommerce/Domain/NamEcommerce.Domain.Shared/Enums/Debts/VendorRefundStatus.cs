namespace NamEcommerce.Domain.Shared.Enums.Debts;

public enum VendorRefundStatus
{
    /// <summary>
    /// Chờ thu tiền — đã ghi nhận khoản NCC cần hoàn, chưa thực hiện thu.
    /// </summary>
    Pending = 10,

    /// <summary>
    /// Đã thu tiền — NCC đã hoàn trả cho shop.
    /// </summary>
    Completed = 20,

    /// <summary>
    /// Đã huỷ — không thực hiện thu tiền.
    /// </summary>
    Cancelled = 30
}
