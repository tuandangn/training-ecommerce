namespace NamEcommerce.Domain.Shared.Enums.Debts;

public enum CustomerRefundStatus
{
    /// <summary>
    /// Chờ hoàn tiền — đã ghi nhận khoản cần hoàn, chưa thực hiện chi trả.
    /// </summary>
    Pending = 10,

    /// <summary>
    /// Đã hoàn tiền — đã chi trả cho khách.
    /// </summary>
    Completed = 20,

    /// <summary>
    /// Đã huỷ — không thực hiện hoàn tiền.
    /// </summary>
    Cancelled = 30
}
