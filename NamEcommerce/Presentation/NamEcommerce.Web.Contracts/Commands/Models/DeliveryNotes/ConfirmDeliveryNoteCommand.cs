using NamEcommerce.Web.Contracts.Models.DeliveryNotes;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

public sealed class ConfirmDeliveryNoteCommand : ICommand<ConfirmDeliveryNoteResultModel>
{
    public Guid DeliveryNoteId { get; init; }
}
