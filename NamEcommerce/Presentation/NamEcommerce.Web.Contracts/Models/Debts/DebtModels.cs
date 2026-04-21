using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Debts;

// ── Trang List: Hiển thị danh sách khách hàng có công nợ ────────────────────

public sealed class CustomerDebtListModel
{
    public string? Keywords { get; set; }
    public IPagedDataModel<CustomerDebtCustomerSummaryModel>? Data { get; set; }
}

/// <summary>Một dòng trong bảng danh sách khách hàng có công nợ.</summary>
public sealed record CustomerDebtCustomerSummaryModel
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền cọc / tiền thừa chưa áp dụng.</summary>
    public decimal DepositBalance { get; init; }
    public int DebtCount { get; init; }
}

// ── Trang Details: Toàn bộ công nợ của 1 khách hàng ─────────────────────────

public sealed class CustomerDebtDetailsModel
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    public decimal DepositBalance { get; init; }

    /// <summary>Danh sách từng phiếu công nợ (kèm lịch sử thanh toán của từng phiếu).</summary>
    public IList<CustomerDebtItemModel> Debts { get; init; } = [];

    /// <summary>Danh sách tiền cọc / tiền thừa chưa áp dụng.</summary>
    public IList<CustomerPaymentListItemModel> Deposits { get; init; } = [];

    /// <summary>Lịch sử 20 giao dịch gần nhất.</summary>
    public IList<CustomerPaymentListItemModel> RecentPayments { get; init; } = [];
}

/// <summary>Một phiếu công nợ trong trang Details của khách hàng.</summary>
public sealed record CustomerDebtItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string DeliveryNoteCode { get; init; }
    public required string OrderCode { get; init; }
    public required Guid OrderId { get; init; }

    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }

    public int Status { get; init; }
    public DateTime? DueDateUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }

    public IList<CustomerPaymentListItemModel> Payments { get; init; } = [];
}

// ── Shared / Payment ─────────────────────────────────────────────────────────

public sealed record CustomerPaymentListItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public int PaymentType { get; init; }
    public string? Note { get; init; }
    public DateTime PaidOnUtc { get; init; }
    public string? OrderCode { get; init; }
    public string? DeliveryNoteCode { get; init; }
    public Guid? CustomerDebtId { get; init; }
}

/// <summary>Model để in biên lai cho 1 lần thanh toán cụ thể.</summary>
public sealed record CustomerPaymentReceiptModel
{
    public required Guid PaymentId { get; init; }
    public required string PaymentCode { get; init; }
    public required string CustomerName { get; init; }
    public required Guid CustomerId { get; init; }

    public int PaymentMethod { get; set; }
    public int PaymentType { get; set; }

    public string? DebtCode { get; init; }
    public string? OrderCode { get; init; }
    public string? DeliveryNoteCode { get; init; }

    public decimal Amount { get; init; }
    public string? Note { get; init; }
    public DateTime PaidOn { get; init; }
}

/// <summary>Form submit khi thu tiền (cả thu riêng lẫn thu linh động).</summary>
public sealed class RecordPaymentModel
{
    public Guid CustomerId { get; set; }
    /// <summary>Nếu có → thu tiền cho 1 phiếu cụ thể. Nếu null → FIFO linh động.</summary>
    public Guid? CustomerDebtId { get; set; }
    public Guid? OrderId { get; set; }

    public decimal Amount { get; set; }
    public int PaymentMethod { get; set; }
    public int PaymentType { get; set; }
    public string? Note { get; set; }
    public DateTime PaidOnUtc { get; set; } = DateTime.UtcNow;
}

// ── Legacy search model (kept for backward compat if needed) ─────────────────

public sealed class CustomerDebtSearchModel
{
    public string? Keywords { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}
