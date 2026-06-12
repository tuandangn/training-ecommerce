using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

public sealed class MarkDeliveringDeliveryNoteCommand : ICommand<CommonActionResultModel>
{
    public Guid DeliveryNoteId { get; init; }
}
