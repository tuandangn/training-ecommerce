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
        decimal totalAmount, DateTime? dueDateUtc, Guid? createdByUserId) : base(Guid.NewGuid())
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
        CreatedByUserId = createdByUserId;
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
    
    public DebtStatus Status { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void ApplyPayment(decimal amount)
    {
        if (amount <= 0) return;

        PaidAmount += amount;
        RemainingAmount = TotalAmount - PaidAmount;

        if (RemainingAmount <= 0)
        {
            RemainingAmount = 0;
            Status = DebtStatus.FullyPaid;
        }
        else
        {
            Status = DebtStatus.PartiallyPaid;
        }

        UpdatedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Đánh dấu phiếu công nợ vừa được khởi tạo — Manager gọi trước <c>InsertAsync</c>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new CustomerDebtCreated(Id, CustomerId, TotalAmount, DeliveryNoteId, OrderId));

    /// <summary>
    /// Đánh dấu phiếu công nợ vừa được cập nhật — raise <see cref="CustomerDebtUpdated"/>.
    /// Nếu sau update <c>Status == FullyPaid</c> thì raise thêm <see cref="CustomerDebtFullyPaid"/>.
    /// </summary>
    internal void MarkUpdated()
    {
        RaiseDomainEvent(new CustomerDebtUpdated(Id));
        if (Status == DebtStatus.FullyPaid)
            RaiseDomainEvent(new CustomerDebtFullyPaid(Id, CustomerId));
    }
}
