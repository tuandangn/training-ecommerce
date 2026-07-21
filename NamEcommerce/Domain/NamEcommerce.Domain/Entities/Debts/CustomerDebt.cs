using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Events.Debts;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record CustomerDebt : AppAggregateEntity
{
    public CustomerDebt(Guid id) : base(id)
    {
        Code = string.Empty;
        CustomerName = string.Empty;
        DeliveryNoteCode = string.Empty;
        OrderCode = string.Empty;
    }

    internal CustomerDebt(string code, Guid customerId, string customerName, 
        Guid deliveryNoteId, string deliveryNoteCode, 
        Guid orderId, string orderCode,
        decimal totalAmount, DateTime? dueDateUtc) : base(Guid.NewGuid())
    {
        Code = code;
        CustomerId = customerId;
        CustomerName = customerName;
        DeliveryNoteId = deliveryNoteId;
        DeliveryNoteCode = deliveryNoteCode;
        OrderId = orderId;
        OrderCode = orderCode;
        TotalAmount = totalAmount;
        RemainingAmount = totalAmount;
        PaidAmount = 0;
        Status = DebtStatus.Outstanding;
        DueDateUtc = dueDateUtc;
        CreatedOnUtc = DateTime.UtcNow;
    }

    /// <summary>Constructor cho công nợ ban đầu (số dư đầu kỳ) — không gắn phiếu xuất hay đơn hàng.</summary>
    internal CustomerDebt(string code, Guid customerId, string customerName,
        decimal totalAmount) : base(Guid.NewGuid())
    {
        Code = code;
        CustomerId = customerId;
        CustomerName = customerName;
        DeliveryNoteId = Guid.Empty;
        DeliveryNoteCode = string.Empty;
        OrderId = Guid.Empty;
        OrderCode = string.Empty;
        TotalAmount = totalAmount;
        RemainingAmount = totalAmount;
        PaidAmount = 0;
        Status = DebtStatus.Outstanding;
        DueDateUtc = null;
        IsOpeningBalance = true;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; }
    public Guid DeliveryNoteId { get; private set; }
    public string DeliveryNoteCode { get; private set; }
    public Guid OrderId { get; private set; }
    public string OrderCode { get; private set; }
    
    public Guid CustomerId { get; private set; }
    public string CustomerName { 
        get;
        internal set
        {
            field = value;
            NormalizedCustomerName = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedCustomerName { get; private set; } = "";
    public string? CustomerPhone { 
        get;
        internal set
        {
            field = value;
            NormalizedCustomerPhone = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedCustomerPhone { get; private set; } = "";
    public string? CustomerAddress { 
        get;
        internal set
        {
            field = value;
            NormalizedCustomerAddress = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedCustomerAddress { get; private set; } = "";

    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }

    public bool IsOpeningBalance { get; private set; }

    public DebtStatus Status { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void UpdateTotalAmount(decimal totalAmount)
    {
        TotalAmount = totalAmount;
        RemainingAmount = Math.Max(0m, totalAmount - PaidAmount);
        Status = RemainingAmount <= 0m
            ? DebtStatus.FullyPaid
            : PaidAmount > 0m ? DebtStatus.PartiallyPaid : DebtStatus.Outstanding;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void MarkCreated()
        => RaiseDomainEvent(new CustomerDebtCreated(Id, CustomerId, TotalAmount, DeliveryNoteId, OrderId));
}
