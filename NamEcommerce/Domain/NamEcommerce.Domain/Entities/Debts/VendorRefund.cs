using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.Debts;

/// <summary>
/// Phiếu thu tiền hoàn từ NCC — sinh tự động khi tổng giá trị trả hàng NCC vượt quá
/// tổng công nợ còn lại của NCC (<c>VendorDebt.RemainingAmount</c> xuống âm).
/// </summary>
[Serializable]
public sealed record VendorRefund : AppAggregateEntity
{
    public VendorRefund(Guid id) : base(id)
    {
        Code = string.Empty;
        VendorName = string.Empty;
        VendorReturnCode = string.Empty;
    }

    internal VendorRefund(
        string code,
        Guid vendorId,
        string vendorName,
        Guid vendorReturnId,
        string vendorReturnCode,
        Guid? vendorDebtId,
        decimal amount,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        VendorId = vendorId;
        VendorName = vendorName;
        VendorReturnId = vendorReturnId;
        VendorReturnCode = vendorReturnCode;
        VendorDebtId = vendorDebtId;
        Amount = amount;
        Status = VendorRefundStatus.Pending;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; }

    public Guid VendorId { get; private set; }
    public string VendorName { get; private set; }

    /// <summary>Phiếu trả hàng NCC tạo ra khoản thu tiền hoàn này.</summary>
    public Guid VendorReturnId { get; private set; }
    public string VendorReturnCode { get; private set; }

    /// <summary>Phiếu công nợ NCC bị âm sau khi áp dụng trả hàng.</summary>
    public Guid? VendorDebtId { get; private set; }

    public decimal Amount { get; private set; }
    public VendorRefundStatus Status { get; private set; }

    public PaymentMethod? PaymentMethod { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public string? Note { get; private set; }

    public DateTime? RefundedOnUtc { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void MarkCreated()
        => RaiseDomainEvent(new VendorRefundCreated(Id, VendorId, Amount));

    /// <summary>
    /// Xác nhận đã thu tiền từ NCC (Pending → Completed).
    /// </summary>
    internal void Complete(PaymentMethod paymentMethod, Guid? bankAccountId, string? note, Guid? completedByUserId)
    {
        if (Status != VendorRefundStatus.Pending)
            throw new NamEcommerceDomainException("Error.VendorRefund.CannotComplete");

        Status = VendorRefundStatus.Completed;
        PaymentMethod = paymentMethod;
        BankAccountId = bankAccountId;
        Note = note;
        CompletedByUserId = completedByUserId;
        RefundedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new VendorRefundCompleted(Id, VendorId, Amount));
    }

    /// <summary>
    /// Huỷ phiếu thu tiền hoàn (Pending → Cancelled). Không thể huỷ nếu đã thu xong.
    /// </summary>
    internal void Cancel()
    {
        if (Status != VendorRefundStatus.Pending)
            throw new NamEcommerceDomainException("Error.VendorRefund.CannotCancel");

        Status = VendorRefundStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new VendorRefundCancelled(Id));
    }
}
