using MediatR;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

public sealed class CreateDeliveryNoteCommand : IRequest<bool>
{
    public CreateDeliveryNoteModel Model { get; init; } = default!;
    public Guid CurrentUserId { get; init; }
}
