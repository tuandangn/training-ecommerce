using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class CompleteMobileDeliveryNoteCommand : IRequest<CommonActionResultModel>
{
    public Guid DeliveryRunId { get; init; }
    public Guid DeliveryNoteId { get; init; }
    public Guid PictureId { get; init; }
    public string? ReceiverName { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? LocationAddress { get; init; }
    public string? Note { get; init; }
    public string? IdempotencyKey { get; init; }
}
