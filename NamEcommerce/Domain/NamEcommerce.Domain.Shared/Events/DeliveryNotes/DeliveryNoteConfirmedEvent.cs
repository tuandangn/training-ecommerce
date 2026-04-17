namespace NamEcommerce.Domain.Shared.Events.DeliveryNotes;

[Serializable]
public sealed class DeliveryNoteConfirmedEvent : BaseEvent
{
    public DeliveryNoteConfirmedEvent(Guid deliveryNoteId)
        => DeliveryNoteId = deliveryNoteId;

    public Guid DeliveryNoteId { get; init; }
}
