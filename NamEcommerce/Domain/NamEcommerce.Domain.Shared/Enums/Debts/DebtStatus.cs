namespace NamEcommerce.Domain.Shared.Enums.Debts;

public enum DebtStatus
{
    /// <summary>
    /// Chưa thanh toán
    /// </summary>
    Outstanding = 10,

    /// <summary>
    /// Thanh toán một phần
    /// </summary>
    PartiallyPaid = 20,

    /// <summary>
    /// Đã thanh toán hết
    /// </summary>
    FullyPaid = 30
}

public enum PaymentType
{
    /// <summary>
    /// Trả nợ cho phiếu xuất
    /// </summary>
    DebtPayment = 10,

    /// <summary>
    /// Tiền cọc cho đơn hàng
    /// </summary>
    Deposit = 20,

    /// <summary>
    /// Thu tiền chung (không gắn đơn)
    /// </summary>
    General = 30
}
