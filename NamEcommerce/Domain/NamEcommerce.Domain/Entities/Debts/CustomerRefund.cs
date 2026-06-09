using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Events.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Entities.Debts;

/// <summary>
/// Phiếu hoàn tiền khách hàng — sinh tự động khi tổng giá trị trả hàng vượt quá
/// tổng công nợ còn lại của khách (<c>CustomerDebt.RemainingAmount</c> xuống âm).
/// </summary>
[Serializable]
public sealed record CustomerRefund : AppAggregateEntity
{
    public CustomerRefund(Guid id) : base(id)
    {
        Code = string.Empty;
        CustomerName = string.Empty;
        CustomerReturnCode = string.Empty;
    }

    internal CustomerRefund(
        string code,
        Guid customerId,
        string customerName,
        Guid customerReturnId,
        string customerReturnCode,
        Guid? customerDebtId,
        decimal amount,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        Code = code;
        CustomerId = customerId;
        CustomerName = customerName;
        CustomerReturnId = customerReturnId;
        CustomerReturnCode = customerReturnCode;
        CustomerDebtId = customerDebtId;
        Amount = amount;
        Status = CustomerRefundStatus.Pending;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; }

    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; }

    /// <summary>Phiếu trả hàng tạo ra khoản hoàn tiền này.</summary>
    public Guid CustomerReturnId { get; private set; }
    public string CustomerReturnCode { get; private set; }

    /// <summary>Phiếu công nợ bị âm sau khi áp dụng trả hàng.</summary>
    public Guid? CustomerDebtId { get; private set; }

    public decimal Amount { get; private set; }
    public CustomerRefundStatus Status { get; private set; }

    public PaymentMethod? PaymentMethod { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public string? Note { get; private set; }

    public DateTime? RefundedOnUtc { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void MarkCreated()
        => RaiseDomainEvent(new CustomerRefundCreated(Id, CustomerId, Amount));

    /// <summary>
    /// Xác nhận đã hoàn tiền cho khách (Pending → Completed).
    /// </summary>
    internal void Complete(PaymentMethod paymentMethod, Guid? bankAccountId, string? note, Guid? completedByUserId)
    {
        if (Status != CustomerRefundStatus.Pending)
            throw new NamEcommerceDomainException("Error.CustomerRefund.CannotComplete");

        Status = CustomerRefundStatus.Completed;
        PaymentMethod = paymentMethod;
        BankAccountId = bankAccountId;
        Note = note;
        CompletedByUserId = completedByUserId;
        RefundedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CustomerRefundCompleted(Id, CustomerId, Amount));
    }

    /// <summary>
    /// Huỷ phiếu hoàn tiền (Pending → Cancelled). Không thể huỷ nếu đã hoàn xong.
    /// </summary>
    internal void Cancel()
    {
        if (Status != CustomerRefundStatus.Pending)
            throw new NamEcommerceDomainException("Error.CustomerRefund.CannotCancel");

        Status = CustomerRefundStatus.Cancelled;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CustomerRefundCancelled(Id));
    }
}
