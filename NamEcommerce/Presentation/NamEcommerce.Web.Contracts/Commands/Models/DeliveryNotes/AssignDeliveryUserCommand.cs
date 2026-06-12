using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed record AssignDeliveryUserCommand : ICommand<CommonActionResultModel>
{
    public required Guid DeliveryNoteId { get; init; }
    public required Guid AssignedDeliveryUserId { get; init; }
}
