namespace NamEcommerce.Domain.Shared.Events.Debts;

/// <summary>
/// Khoản thanh toán khách hàng vừa được ghi nhận (DebtPayment / Deposit / OrderPayment...).
/// </summary>
public sealed record CustomerPaymentRecorded(
    Guid CustomerPaymentId,
    Guid CustomerId,
    decimal Amount,
    Guid? CustomerDebtId) : DomainEvent;
