using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

/// <summary>Tổng hợp công nợ theo từng nhà cung cấp — dùng cho trang List.</summary>
[Serializable]
public sealed record VendorDebtSummaryDto
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorAddress { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền ứng trước cho NCC chưa được áp dụng vào nợ.</summary>
    public decimal AdvanceBalance { get; init; }
    public int DebtCount { get; init; }
}

/// <summary>Toàn bộ thông tin công nợ của 1 nhà cung cấp — dùng cho trang Details.</summary>
[Serializable]
public sealed record VendorDebtsByVendorDto
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền ứng trước chưa áp dụng.</summary>
    public decimal AdvanceBalance { get; init; }
    /// <summary>Danh sách từng phiếu công nợ (kèm payments của từng phiếu).</summary>
    public IList<VendorDebtDto> Debts { get; init; } = [];
    /// <summary>Danh sách các khoản ứng trước chưa áp dụng.</summary>
    public IList<VendorPaymentDto> AdvancePayments { get; init; } = [];
    /// <summary>Lịch sử thanh toán gần nhất (tất cả loại).</summary>
    public IList<VendorPaymentDto> RecentPayments { get; init; } = [];
}

[Serializable]
public sealed record VendorDebtDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }

    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorAddress { get; init; }

    public required Guid PurchaseOrderId { get; init; }
    public required string PurchaseOrderCode { get; init; }

    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }

    public DebtStatus Status { get; init; }
    public DateTime? DueDateUtc { get; init; }

    public DateTime CreatedOnUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public IList<VendorPaymentDto> Payments { get; init; } = [];
}

[Serializable]
public sealed record CreateVendorDebtDto
{
    public required Guid VendorId { get; init; }
    public required Guid PurchaseOrderId { get; init; }
    public required decimal TotalAmount { get; init; }
    public DateTime? DueDateUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new ArgumentException("VendorId is required");
        if (PurchaseOrderId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderId is required");
        if (TotalAmount <= 0)
            throw new ArgumentException("TotalAmount must be greater than 0");
    }
}

[Serializable]
public sealed record VendorPaymentDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }

    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }

    public Guid? VendorDebtId { get; init; }

    public Guid? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }

    public decimal Amount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public PaymentType PaymentType { get; init; }
    public string? Note { get; init; }

    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateVendorPaymentDto
{
    public required Guid VendorId { get; init; }

    public Guid? VendorDebtId { get; init; }
    public Guid? PurchaseOrderId { get; init; }

    public decimal Amount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public PaymentType PaymentType { get; init; }
    public string? Note { get; init; }

    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new ArgumentException("VendorId is required");
        if (Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");
        if (PaidOnUtc == default)
            throw new ArgumentException("PaidOnUtc is required");
    }
}
