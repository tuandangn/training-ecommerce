using NamEcommerce.Web.Contracts.Models.DeliveryNotes;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class ConfirmDeliveryNoteCommand : ICommand<ConfirmDeliveryNoteResultModel>
{
    public Guid DeliveryNoteId { get; init; }
}
