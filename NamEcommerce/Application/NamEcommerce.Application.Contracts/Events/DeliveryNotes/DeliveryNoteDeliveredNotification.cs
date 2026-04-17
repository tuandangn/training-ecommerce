using MediatR;

namespace NamEcommerce.Application.Contracts.Events.DeliveryNotes;

[Serializable]
public sealed record DeliveryNoteDeliveredNotification(Guid DeliveryNoteId) : INotification;
