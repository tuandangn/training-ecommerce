namespace NamEcommerce.Domain.Shared.Events.DeliveryNotes;

[Serializable]
public sealed class DeliveryNoteDeliveredEvent : BaseEvent
{
    public DeliveryNoteDeliveredEvent(Guid deliveryNoteId)
        => DeliveryNoteId = deliveryNoteId;

    public Guid DeliveryNoteId { get; init; }
}
