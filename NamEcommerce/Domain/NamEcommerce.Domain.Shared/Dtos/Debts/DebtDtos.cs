using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

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
            throw new ArgumentException("CustomerId is required");
        if (DeliveryNoteId == Guid.Empty)
            throw new ArgumentException("DeliveryNoteId is required");
        if (TotalAmount <= 0)
            throw new ArgumentException("TotalAmount must be greater than 0");
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
            throw new ArgumentException("CustomerId is required");
        if (Amount <= 0)
            throw new ArgumentException("Amount must be greater than 0");
        if (PaidOnUtc == default)
            throw new ArgumentException("PaidOnUtc is required");
    }
}
