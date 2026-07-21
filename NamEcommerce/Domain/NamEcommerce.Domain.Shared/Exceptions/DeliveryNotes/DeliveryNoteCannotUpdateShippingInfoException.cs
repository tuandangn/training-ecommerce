namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class DeliveryNoteCannotUpdateShippingInfoException : NamEcommerceDomainException
{
    public DeliveryNoteCannotUpdateShippingInfoException()
        : base("Error.DeliveryNoteCannotUpdateShippingInfo")
    {
    }
}

