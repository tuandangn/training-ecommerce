namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public sealed record CompleteQuickCreateOrderPaymentAppDto
{
    public required Guid OrderId { get; init; }
    public required decimal PaidAmount { get; init; }
    public required Guid? PaymentIntentId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (PaidAmount < 0)
            return (false, "Error.PaidAmountCannotBeNegative");

        return (true, null);
    }
}

