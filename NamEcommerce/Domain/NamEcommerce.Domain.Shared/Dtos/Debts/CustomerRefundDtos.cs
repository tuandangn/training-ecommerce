using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.Debts;

[Serializable]
public sealed record CustomerRefundDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }

    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }

    public required Guid CustomerReturnId { get; init; }
    public required string CustomerReturnCode { get; init; }

    public Guid? CustomerDebtId { get; init; }

    public decimal Amount { get; init; }
    public CustomerRefundStatus Status { get; init; }
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
public sealed record CreateCustomerRefundDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required Guid CustomerReturnId { get; init; }
    public required string CustomerReturnCode { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public required decimal Amount { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRefund.CustomerRequired");
        if (CustomerReturnId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerRefund.CustomerReturnRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.CustomerRefund.AmountMustBePositive");
    }
}
