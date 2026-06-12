using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

public sealed class CancelDeliveryNoteCommand : ICommand<Unit>
{
    public Guid DeliveryNoteId { get; init; }
}
