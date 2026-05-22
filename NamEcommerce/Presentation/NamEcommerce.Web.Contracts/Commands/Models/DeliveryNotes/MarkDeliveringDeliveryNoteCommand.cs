using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

public sealed class MarkDeliveringDeliveryNoteCommand : IRequest<Unit>
{
    public Guid DeliveryNoteId { get; init; }
}
