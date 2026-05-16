namespace NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;

/// <summary>
/// Thrown when attempting to cancel a DeliveryNote that has at least one Confirmed CustomerReturn.
/// The return must be processed (e.g., reversed) before the delivery note can be cancelled.
/// </summary>
public sealed class DeliveryNoteHasConfirmedReturnsException : NamEcommerceDomainException
{
    public DeliveryNoteHasConfirmedReturnsException(Guid deliveryNoteId)
        : base("Error.DeliveryNoteHasConfirmedReturns", deliveryNoteId)
    {
    }
}
