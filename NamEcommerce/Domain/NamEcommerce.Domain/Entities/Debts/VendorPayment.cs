using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events.Debts;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record VendorPayment : AppAggregateEntity
{
    public VendorPayment(Guid id) : base(id)
    {
        Code = string.Empty;
        VendorName = string.Empty;
    }

    internal VendorPayment(string code, Guid vendorId, string vendorName,
        decimal amount, PaymentMethod paymentMethod, PaymentType paymentType,
        DateTime paidOnUtc, Guid? recordedByUserId, string? note) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        Amount = amount;
        PaymentMethod = paymentMethod;
        PaymentType = paymentType;
        PaidOnUtc = paidOnUtc;
        RecordedByUserId = recordedByUserId;
        Note = note;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; }

    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; }

    public Guid? VendorDebtId { get; internal set; }

    public Guid? PurchaseOrderId { get; internal set; }
    public string? PurchaseOrderCode { get; internal set; }

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

    /// <summary>
    /// Đánh dấu khoản thanh toán NCC vừa được ghi nhận — Manager gọi trước <c>InsertAsync</c>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new VendorPaymentRecorded(Id, VendorId, Amount, VendorDebtId));
}
