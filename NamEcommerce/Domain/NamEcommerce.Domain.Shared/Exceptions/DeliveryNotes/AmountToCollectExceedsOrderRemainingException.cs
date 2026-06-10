namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class AmountToCollectExceedsOrderRemainingException : NamEcommerceDomainException
{
    public AmountToCollectExceedsOrderRemainingException(decimal requested, decimal remaining)
        : base("Error.AmountToCollectExceedsOrderRemaining", requested, remaining)
    {
    }
}
