namespace NamEcommerce.Domain.Shared.Events.Debts;

/// <summary>
/// Khoản thanh toán NCC vừa được ghi nhận.
/// </summary>
public sealed record VendorPaymentRecorded(
    Guid VendorPaymentId,
    Guid VendorId,
    decimal Amount,
    Guid? VendorDebtId) : DomainEvent;
