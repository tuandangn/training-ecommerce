using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

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
    public decimal DepositBalance { get; init; }
    public int DebtCount { get; init; }
}

[Serializable]
public sealed record CustomerDebtsByCustomerDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public decimal TotalDebtAmount { get; init; }
    public decimal TotalPaidAmount { get; init; }
    public decimal TotalRemainingAmount { get; init; }
    public decimal DepositBalance { get; init; }
    public IList<CustomerDebtDto> Debts { get; init; } = [];
    public IList<CustomerPaymentDto> Deposits { get; init; } = [];
    public IList<CustomerPaymentDto> RecentPayments { get; init; } = [];
    public decimal UnappliedCreditNoteBalance { get; init; }
    public IList<CustomerCreditNoteDto> UnappliedCreditNotes { get; init; } = [];
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

    public IList<CustomerPaymentDto> Payments { get; init; } = [];
    public IList<CustomerCreditNoteAllocationDto> CreditNoteAllocations { get; init; } = [];
}

[Serializable]
public sealed record CustomerCreditNoteDto
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
    public CreditNoteStatus Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public IList<CustomerCreditNoteAllocationDto> Allocations { get; init; } = [];
}

[Serializable]
public sealed record CustomerCreditNoteAllocationDto
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
public sealed record CreateCustomerDebtDto
{
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required decimal TotalAmount { get; init; }
    public DateTime? DueDateUtc { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRequired");
        if (DeliveryNoteId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.DeliveryNoteCodeRequired");
        if (TotalAmount <= 0)
            throw new NamEcommerceDomainException("Error.TotalAmountMustBePositive");
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
public sealed record CreateInitialCustomerDebtDto
{
    public required Guid CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRequired");
        if (TotalAmount <= 0)
            throw new NamEcommerceDomainException("Error.TotalAmountMustBePositive");
    }
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
            throw new NamEcommerceDomainException("Error.CustomerRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.PaymentAmountMustBePositive");
        if (PaidOnUtc == default)
            throw new NamEcommerceDomainException("Error.PaymentDateRequired");
    }
}
