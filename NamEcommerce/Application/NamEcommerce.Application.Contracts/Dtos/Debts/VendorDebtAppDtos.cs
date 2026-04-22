namespace NamEcommerce.Application.Contracts.Dtos.Debts;

/// <summary>Tổng hợp công nợ theo từng nhà cung cấp — dùng cho trang List.</summary>
[Serializable]
public sealed record VendorDebtSummaryAppDto
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
public sealed record VendorDebtsByVendorAppDto
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền ứng trước chưa áp dụng.</summary>
    public decimal AdvanceBalance { get; init; }
    /// <summary>Danh sách từng phiếu công nợ (kèm payments của từng phiếu).</summary>
    public IList<VendorDebtAppDto> Debts { get; init; } = [];
    /// <summary>Danh sách các khoản ứng trước chưa áp dụng.</summary>
    public IList<VendorPaymentAppDto> AdvancePayments { get; init; } = [];
    /// <summary>Lịch sử thanh toán gần nhất (tất cả loại).</summary>
    public IList<VendorPaymentAppDto> RecentPayments { get; init; } = [];
}

[Serializable]
public sealed record VendorDebtAppDto
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

    public int Status { get; init; }
    public DateTime? DueDateUtc { get; init; }

    public DateTime CreatedOnUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public IList<VendorPaymentAppDto> Payments { get; init; } = [];
}

[Serializable]
public sealed record VendorPaymentAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }

    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }

    public Guid? VendorDebtId { get; init; }

    public Guid? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }

    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public int PaymentType { get; init; }
    public string? Note { get; init; }

    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateVendorDebtAppDto
{
    public required Guid VendorId { get; init; }
    public required Guid PurchaseOrderId { get; init; }
    public required decimal TotalAmount { get; init; }
    public DateTime? DueDateUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (VendorId == Guid.Empty)
            return (false, "Error.VendorRequired");
        if (PurchaseOrderId == Guid.Empty)
            return (false, "Error.PurchaseOrderRequired");
        if (TotalAmount <= 0)
            return (false, "Error.PaymentAmountMustBePositive");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record CreateVendorPaymentAppDto
{
    public required Guid VendorId { get; init; }

    public Guid? VendorDebtId { get; init; }
    public Guid? PurchaseOrderId { get; init; }

    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public int PaymentType { get; init; }
    public string? Note { get; init; }

    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (VendorId == Guid.Empty)
            return (false, "Error.VendorRequired");
        if (Amount <= 0)
            return (false, "Error.PaymentAmountMustBePositive");
        if (PaidOnUtc == default)
            return (false, "Error.PaymentDateRequired");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record CreateVendorDebtResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
    public VendorDebtAppDto? Debt { get; init; }
}

[Serializable]
public sealed record RecordVendorPaymentResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
    /// <summary>Kết quả của thao tác đơn lẻ (RecordPayment / RecordAdvancePayment).</summary>
    public VendorPaymentAppDto? Payment { get; init; }
    /// <summary>Kết quả của thao tác FIFO (RecordFlexiblePayment) — có thể nhiều payment.</summary>
    public IList<VendorPaymentAppDto> Payments { get; init; } = [];
}
