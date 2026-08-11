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
        decimal totalAmount, DateTime? dueDateUtc) : base(Guid.NewGuid())
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
        CreatedOnUtc = DateTime.UtcNow;
    }

    internal VendorDebt(string code, Guid vendorId, string vendorName, 
        decimal totalAmount, DateTime? dueDateUtc,
        Guid goodsReceiptId, string goodsReceiptCode) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        GoodsReceiptId = goodsReceiptId;
        GoodsReceiptCode = goodsReceiptCode;
        TotalAmount = totalAmount;
        RemainingAmount = totalAmount;
        PaidAmount = 0;
        Status = DebtStatus.Outstanding;
        DueDateUtc = dueDateUtc;
        CreatedOnUtc = DateTime.UtcNow;
    }

    internal VendorDebt(string code, Guid vendorId, string vendorName,
        decimal totalAmount) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        TotalAmount = totalAmount;
        RemainingAmount = totalAmount;
        PaidAmount = 0;
        Status = DebtStatus.Outstanding;
        DueDateUtc = null;
        IsOpeningBalance = true;
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
    public string? GoodsReceiptCode { get; set; }

    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }

    public bool IsOpeningBalance { get; private set; }

    public DebtStatus Status { get; private set; }
    public DateTime? DueDateUtc { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void ChangeDueDate(DateTime? newDueDateUtc)
    {
        DueDateUtc = newDueDateUtc;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void MarkCreated()
        => RaiseDomainEvent(new VendorDebtCreated(Id, VendorId, TotalAmount, PurchaseOrderId, GoodsReceiptId));
}
