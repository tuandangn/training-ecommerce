using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

/// <summary>Tổng hợp công nợ theo từng khách hàng — dùng cho trang List.</summary>
[Serializable]
public sealed record CustomerDebtSummaryDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền cọc / tiền thừa chưa được áp dụng vào nợ.</summary>
    public decimal DepositBalance { get; init; }
    public int DebtCount { get; init; }
}

/// <summary>Toàn bộ thông tin công nợ của 1 khách hàng — dùng cho trang Details.</summary>
[Serializable]
public sealed record CustomerDebtsByCustomerDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền cọc / tiền thừa chưa áp dụng.</summary>
    public decimal DepositBalance { get; init; }
    /// <summary>Danh sách từng phiếu công nợ (kèm payments của từng phiếu).</summary>
    public IList<CustomerDebtDto> Debts { get; init; } = [];
    /// <summary>Danh sách các khoản tiền cọc chưa áp dụng.</summary>
    public IList<CustomerPaymentDto> Deposits { get; init; } = [];
    /// <summary>Lịch sử thanh toán gần nhất (tất cả loại).</summary>
    public IList<CustomerPaymentDto> RecentPayments { get; init; } = [];
}

[Serializable]
public sealed record CustomerDebtDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    
    public required Guid DeliveryNoteId { get; init; }
    public required string DeliveryNoteCode { get; init; }
    
    public required Guid OrderId { get; init; }
    public required string OrderCode { get; init; }

    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    
    public DebtStatus Status { get; init; }
    public DateTime? DueDateUtc { get; init; }
    
    public DateTime CreatedOnUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public IList<CustomerPaymentDto> Payments { get; init; } = [];
}

[Serializable]
public sealed record CreateCustomerDebtDto
{
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required decimal TotalAmount { get; init; }
    public DateTime? DueDateUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new ArgumentException("Mã khách hàng không được để trống");
        if (DeliveryNoteId == Guid.Empty)
            throw new ArgumentException("Mã phiếu giao hàng không được để trống");
        if (TotalAmount <= 0)
            throw new ArgumentException("Tổng tiền phải lớn hơn 0");
    }
}

[Serializable]
public sealed record CustomerPaymentDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    
    public Guid? OrderId { get; init; }
    public string? OrderCode { get; init; }
    
    public Guid? DeliveryNoteId { get; init; }
    public string? DeliveryNoteCode { get; init; }
    
    public Guid? CustomerDebtId { get; init; }

    public decimal Amount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public PaymentType PaymentType { get; init; }
    public string? Note { get; init; }
    
    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateCustomerPaymentDto
{
    public required Guid CustomerId { get; init; }
    
    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }

    public decimal Amount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public PaymentType PaymentType { get; init; }
    public string? Note { get; init; }
    
    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new ArgumentException("Mã khách hàng không được để trống");
        if (Amount <= 0)
            throw new ArgumentException("Số tiền thanh toán phải lớn hơn 0");
        if (PaidOnUtc == default)
            throw new ArgumentException("Ngày thanh toán không được để trống");
    }
}
