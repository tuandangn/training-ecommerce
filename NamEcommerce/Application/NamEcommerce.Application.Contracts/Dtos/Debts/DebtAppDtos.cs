namespace NamEcommerce.Application.Contracts.Dtos.Debts;

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
    public Guid? CreatedByUserId { get; init; }

    public IList<CustomerPaymentAppDto> Payments { get; init; } = [];
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
    public string? Note { get; init; }
    
    public DateTime PaidOnUtc { get; init; }
    public Guid? RecordedByUserId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (CustomerId == Guid.Empty)
            return (false, "CustomerId is required.");
        if (Amount <= 0)
            return (false, "Amount must be greater than 0.");
        if (PaidOnUtc == default)
            return (false, "PaidOnUtc is required.");

        return (true, string.Empty);
    }
}
