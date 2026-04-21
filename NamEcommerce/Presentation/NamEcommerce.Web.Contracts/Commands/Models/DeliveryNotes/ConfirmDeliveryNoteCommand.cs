using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

public sealed class ConfirmDeliveryNoteCommand : IRequest<CommonActionResultModel>
{
    public Guid DeliveryNoteId { get; init; }
}
