namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

public sealed class DeliveryNoteNotFoundException : Exception
{
    public DeliveryNoteNotFoundException(Guid id)
        : base($"Delivery note with ID {id} was not found.")
    {
    }
}
