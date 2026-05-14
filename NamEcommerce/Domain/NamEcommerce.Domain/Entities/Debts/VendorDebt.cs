using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Events.Debts;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record VendorDebt : AppAggregateEntity
{
    public VendorDebt(Guid id) : base(id)
    {
        Code = string.Empty;
        VendorName = string.Empty;
    }

    internal VendorDebt(string code, Guid vendorId, string vendorName,
        Guid purchaseOrderId, string purchaseOrderCode,
        decimal totalAmount, DateTime? dueDateUtc, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        PurchaseOrderId = purchaseOrderId;
        PurchaseOrderCode = purchaseOrderCode;
        TotalAmount = totalAmount;
        RemainingAmount = totalAmount;
        PaidAmount = 0;
        Status = DebtStatus.Outstanding;
        DueDateUtc = dueDateUtc;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    internal VendorDebt(string code, Guid vendorId, string vendorName,
        Guid goodsReceiptId,
        decimal totalAmount, DateTime? dueDateUtc, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        GoodsReceiptId = goodsReceiptId;
        TotalAmount = totalAmount;
        RemainingAmount = totalAmount;
        PaidAmount = 0;
        Status = DebtStatus.Outstanding;
        DueDateUtc = dueDateUtc;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    internal VendorDebt(string code, Guid vendorId, string vendorName,
        decimal totalAmount, Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        TotalAmount = totalAmount;
        RemainingAmount = totalAmount;
        PaidAmount = 0;
        Status = DebtStatus.Outstanding;
        DueDateUtc = null;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; }

    public Guid VendorId { get; private set; }

    public string VendorName
    {
        get;
        internal set
        {
            field = value;
            NormalizedVendorName = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedVendorName { get; private set; } = "";

    public string? VendorPhone
    {
        get;
        internal set
        {
            field = value;
            NormalizedVendorPhone = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedVendorPhone { get; private set; } = "";

    public string? VendorAddress
    {
        get;
        internal set
        {
            field = value;
            NormalizedVendorAddress = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedVendorAddress { get; private set; } = "";

    public Guid? PurchaseOrderId { get; private set; }
    public string? PurchaseOrderCode { get; private set; }

    public Guid? GoodsReceiptId { get; private set; }

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

    internal void MarkAsPaid()
    {
        PaidAmount = TotalAmount;
        RemainingAmount = 0;
        Status = DebtStatus.FullyPaid;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void ChangeDueDate(DateTime? newDueDateUtc)
    {
        DueDateUtc = newDueDateUtc;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void ApplyReturn(decimal amount, Guid returnId)
    {
        if (amount <= 0) return;

        var wasNonNegative = RemainingAmount >= 0;
        RemainingAmount -= amount;
        UpdatedOnUtc = DateTime.UtcNow;

        if (RemainingAmount <= 0)
            Status = DebtStatus.FullyPaid;

        if (wasNonNegative && RemainingAmount < 0)
        {
            var overAmount = -RemainingAmount;
            RaiseDomainEvent(new VendorDebtBecameNegative(Id, VendorId, returnId, overAmount, RemainingAmount));
        }
    }

    internal void ReverseReturn(decimal amount)
    {
        if (amount <= 0) return;

        RemainingAmount += amount;
        UpdatedOnUtc = DateTime.UtcNow;

        if (RemainingAmount > 0)
            Status = PaidAmount > 0 ? DebtStatus.PartiallyPaid : DebtStatus.Outstanding;
    }

    internal void MarkCreated()
        => RaiseDomainEvent(new VendorDebtCreated(Id, VendorId, TotalAmount, PurchaseOrderId, GoodsReceiptId));

    internal void MarkUpdated()
    {
        RaiseDomainEvent(new VendorDebtUpdated(Id));
        if (Status == DebtStatus.FullyPaid)
            RaiseDomainEvent(new VendorDebtFullyPaid(Id, VendorId));
    }
}
