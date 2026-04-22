namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class DeliveryNoteNotFoundException : NamEcommerceDomainException
{
    public DeliveryNoteNotFoundException(Guid id)
        : base("Error.DeliveryNoteNotFound", id)
    {
    }
}

