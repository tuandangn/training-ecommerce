using MediatR;

namespace NamEcommerce.Application.Contracts.Events.DeliveryNotes;

[Serializable]
public sealed class DeliveryNoteConfirmedNotification : INotification
{
    public DeliveryNoteConfirmedNotification(Guid id)
        => Id = id;

    public Guid Id { get; init; }
}
