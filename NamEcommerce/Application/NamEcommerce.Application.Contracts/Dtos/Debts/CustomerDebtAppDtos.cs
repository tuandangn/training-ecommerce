namespace NamEcommerce.Application.Contracts.Dtos.Debts;

[Serializable]
public sealed record CustomerDebtSummaryAppDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? CustomerAddress { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    /// <summary>Tiền cọc / tiền thừa chưa áp dụng vào nợ.</summary>
    public decimal DepositBalance { get; init; }
    public int DebtCount { get; init; }
}

/// <summary>Toàn bộ thông tin công nợ của 1 khách hàng — dùng cho trang Details.</summary>
[Serializable]
public sealed record CustomerDebtsByCustomerAppDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    public decimal DepositBalance { get; init; }
    public IList<CustomerDebtAppDto> Debts { get; init; } = [];
    public IList<CustomerPaymentAppDto> Deposits { get; init; } = [];
    public IList<CustomerPaymentAppDto> RecentPayments { get; init; } = [];
    public decimal UnappliedCreditNoteBalance { get; init; }
    public IList<CustomerCreditNoteAppDto> UnappliedCreditNotes { get; init; } = [];
}

[Serializable]
public sealed record CustomerDebtAppDto
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

    public int Status { get; init; }
    public DateTime? DueDateUtc { get; init; }

    public DateTime CreatedOnUtc { get; init; }

    public IList<CustomerPaymentAppDto> Payments { get; init; } = [];
    public IList<CustomerCreditNoteAllocationAppDto> CreditNoteAllocations { get; init; } = [];
}

[Serializable]
public sealed record CustomerCreditNoteAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public Guid? SourceDeliveryNoteId { get; init; }
    public decimal Amount { get; init; }
    public decimal AppliedAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public int Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public IList<CustomerCreditNoteAllocationAppDto> Allocations { get; init; } = [];
}

[Serializable]
public sealed record CustomerCreditNoteAllocationAppDto
{
    public required Guid Id { get; init; }
    public required Guid CustomerCreditNoteId { get; init; }
    public required string CustomerCreditNoteCode { get; init; }
    public required Guid SourceReturnId { get; init; }
    public required string SourceReturnCode { get; init; }
    public required Guid CustomerDebtId { get; init; }
    public required string CustomerDebtCode { get; init; }
    public decimal Amount { get; init; }
    public DateTime AppliedOnUtc { get; init; }
    public Guid? AppliedByUserId { get; init; }
    public DateTime? ReversedOnUtc { get; init; }
    public string? ReverseReason { get; init; }
}

[Serializable]
public sealed record CustomerPaymentAppDto
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
    public int PaymentMethod { get; init; }
    public int PaymentType { get; init; }
    public Guid? BankAccountId { get; init; }
    public string? Note { get; init; }

    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateCustomerPaymentAppDto
{
    public required Guid CustomerId { get; init; }

    public Guid? OrderId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public Guid? CustomerDebtId { get; init; }

    public decimal Amount { get; init; }
    public int PaymentMethod { get; init; }
    public int PaymentType { get; init; }
    public Guid? BankAccountId { get; init; }
    public string? Note { get; init; }

    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (CustomerId == Guid.Empty)
            return (false, "Error.CustomerRequired");
        if (Amount <= 0)
            return (false, "Error.PaymentAmountMustBePositive");
        if (PaidOnUtc == default)
            return (false, "Error.PaymentDateRequired");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record CreateInitialCustomerDebtAppDto
{
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }

    public (bool success, string? errorMessage) Validate()
    {
        if (CustomerId == Guid.Empty)
            return (false, "Error.CustomerRequired");
        if (TotalAmount <= 0)
            return (false, "Error.TotalAmountMustBePositive");

        return (true, string.Empty);
    }
}
[Serializable]
public sealed record CreateInitialCustomerDebtResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public CustomerDebtAppDto? Debt { get; init; }
}
