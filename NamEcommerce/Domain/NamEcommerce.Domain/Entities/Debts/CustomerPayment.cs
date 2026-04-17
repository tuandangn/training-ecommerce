using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record CustomerPayment : AppAggregateEntity
{
    public CustomerPayment(Guid id) : base(id)
    {
        Code = string.Empty;
        CustomerName = string.Empty;
    }

    internal CustomerPayment(string code, Guid customerId, string customerName, 
        decimal amount, PaymentMethod paymentMethod, PaymentType paymentType, 
        DateTime paidOnUtc, Guid? recordedByUserId, string? note) : base(Guid.NewGuid())
    {
        Code = code;
        CustomerId = customerId;
        CustomerName = customerName;
        Amount = amount;
        PaymentMethod = paymentMethod;
        PaymentType = paymentType;
        PaidOnUtc = paidOnUtc;
        RecordedByUserId = recordedByUserId;
        Note = note;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; }
    
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; }
    
    public Guid? OrderId { get; internal set; }
    public string? OrderCode { get; internal set; }
    
    public Guid? DeliveryNoteId { get; internal set; }
    public string? DeliveryNoteCode { get; internal set; }
    
    public Guid? CustomerDebtId { get; internal set; }

    public decimal Amount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public string? Note { get; private set; }
    
    public DateTime PaidOnUtc { get; private set; }
    public Guid? RecordedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    public bool IsApplied { get; private set; }
    public DateTime? AppliedOnUtc { get; private set; }

    internal void MarkAsApplied()
    {
        IsApplied = true;
        AppliedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
