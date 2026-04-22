namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class DeliveryProofRequiredException : NamEcommerceDomainException
{
    public DeliveryProofRequiredException()
        : base("Error.DeliveryProofRequired")
    {
    }
}

