using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

[Serializable]
public sealed record VendorRefundDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }

    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }

    public required Guid VendorReturnId { get; init; }
    public required string VendorReturnCode { get; init; }

    public Guid? VendorDebtId { get; init; }

    public decimal Amount { get; init; }
    public VendorRefundStatus Status { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public Guid? BankAccountId { get; init; }
    public string? Note { get; init; }

    public DateTime? RefundedOnUtc { get; init; }
    public Guid? CompletedByUserId { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateVendorRefundDto
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required Guid VendorReturnId { get; init; }
    public required string VendorReturnCode { get; init; }
    public Guid? VendorDebtId { get; init; }
    public required decimal Amount { get; init; }

    public void Verify()
    {
        if (VendorId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.VendorRefund.VendorRequired");
        if (VendorReturnId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.VendorRefund.VendorReturnRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.VendorRefund.AmountMustBePositive");
    }
}
