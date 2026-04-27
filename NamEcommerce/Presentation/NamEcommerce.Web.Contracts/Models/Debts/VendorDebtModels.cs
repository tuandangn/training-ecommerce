using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Debts;

// ── Trang List: Danh sách NCC có công nợ ────────────────────────────────────

public sealed class VendorDebtListModel
{
    public string? Keywords { get; set; }
    public IPagedDataModel<VendorDebtVendorSummaryModel>? Data { get; set; }
}

/// <summary>Một dòng trong bảng danh sách NCC có công nợ.</summary>
public sealed record VendorDebtVendorSummaryModel
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorAddress { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền ứng trước chưa áp dụng vào phiếu nợ.</summary>
    public decimal AdvanceBalance { get; init; }
    public int DebtCount { get; init; }
}

// ── Trang Details: Toàn bộ công nợ của 1 NCC ────────────────────────────────

public sealed class VendorDebtDetailsModel
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền ứng trước cho NCC chưa áp dụng vào phiếu nợ.</summary>
    public decimal AdvanceBalance { get; init; }

    /// <summary>Danh sách từng phiếu công nợ (kèm lịch sử thanh toán).</summary>
    public IList<VendorDebtItemModel> Debts { get; init; } = [];

    /// <summary>Danh sách tiền ứng trước chưa áp dụng.</summary>
    public IList<VendorPaymentListItemModel> AdvancePayments { get; init; } = [];

    /// <summary>Lịch sử giao dịch gần nhất.</summary>
    public IList<VendorPaymentListItemModel> RecentPayments { get; init; } = [];
}

/// <summary>Một phiếu công nợ NCC trong trang Details.</summary>
public sealed record VendorDebtItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string? PurchaseOrderCode { get; init; }
    public required Guid? PurchaseOrderId { get; init; }

    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }

    public int Status { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime CreatedOn { get; init; }

    public IList<VendorPaymentListItemModel> Payments { get; init; } = [];
}

// ── Shared / Payment ─────────────────────────────────────────────────────────

public sealed record VendorPaymentListItemModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public int PaymentType { get; init; }
    public string? Note { get; init; }
    public DateTime PaidOn { get; init; }
    public string? PurchaseOrderCode { get; init; }
    public Guid? VendorDebtId { get; init; }
}

/// <summary>Model để in phiếu chi cho 1 lần thanh toán NCC.</summary>
public sealed record VendorPaymentReceiptModel
{
    public required Guid PaymentId { get; init; }
    public required string PaymentCode { get; init; }
    public required string VendorName { get; init; }
    public required Guid VendorId { get; init; }

    public int PaymentMethod { get; set; }
    public int PaymentType { get; set; }

    public string? DebtCode { get; init; }
    public string? PurchaseOrderCode { get; init; }

    public decimal Amount { get; init; }
    public string? Note { get; init; }
    public DateTime PaidOn { get; init; }
}

/// <summary>Form submit khi chi tiền cho NCC (trả nợ riêng lẻ, FIFO, hoặc ứng trước).</summary>
public sealed class RecordVendorPaymentModel
{
    public Guid VendorId { get; set; }
    /// <summary>Nếu có → chi tiền cho 1 phiếu cụ thể. Nếu null → FIFO hoặc ứng trước.</summary>
    public Guid? VendorDebtId { get; set; }
    public Guid? PurchaseOrderId { get; set; }

    public decimal Amount { get; set; }
    public int PaymentMethod { get; set; }
    public int PaymentType { get; set; }
    public string? Note { get; set; }
    public DateTime PaidOn { get; set; } = DateTime.Now;
}
