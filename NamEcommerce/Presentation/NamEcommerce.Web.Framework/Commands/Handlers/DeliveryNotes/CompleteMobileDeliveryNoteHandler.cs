using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class CompleteMobileDeliveryNoteHandler(IDeliveryNoteAppService deliveryNoteAppService)
    : IRequestHandler<CompleteMobileDeliveryNoteCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CompleteMobileDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        var acceptance = await BuildAcceptanceAsync(request).ConfigureAwait(false);
        if (!acceptance.Success)
            return new CommonActionResultModel { Success = false, ErrorMessage = acceptance.ErrorMessage };

        var result = await deliveryNoteAppService.MarkDeliveredAsync(new MarkDeliveryNoteDeliveredAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            PictureIds = [request.PictureId],
            ReceiverName = request.ReceiverName,
            Acceptance = acceptance.Acceptance,
            CompletionMetadata = new DeliveryCompletionMetadataAppDto
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                LocationAddress = request.LocationAddress,
                Note = request.Note,
                Source = "MobilePwa",
                IdempotencyKey = request.IdempotencyKey,
                CashCollectedAmount = request.CashCollectedAmount ?? 0
            }
        }).ConfigureAwait(false);
        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            SuccessMessage = result.Success ? "Msg.SaveSuccess" : null
        };
    }

    private async Task<(bool Success, string? ErrorMessage, DeliveryAcceptanceAppDto? Acceptance)> BuildAcceptanceAsync(
        CompleteMobileDeliveryNoteCommand request)
    {
        if (request.Items.Count == 0)
            return (true, null, null);

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(request.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return (false, "Error.DeliveryNoteNotFound", null);

        var itemsById = deliveryNote.Items.ToDictionary(item => item.Id);
        var acceptanceItems = new List<DeliveryAcceptanceItemAppDto>(request.Items.Count);
        foreach (var item in request.Items)
        {
            if (!itemsById.TryGetValue(item.DeliveryNoteItemId, out var deliveryNoteItem))
                return (false, "Error.DeliveryAcceptance.InvalidItem", null);

            var returnedQuantity = Math.Max(0m, Math.Min(deliveryNoteItem.Quantity, item.ReturnedQuantity));
            acceptanceItems.Add(new DeliveryAcceptanceItemAppDto
            {
                DeliveryNoteItemId = item.DeliveryNoteItemId,
                AcceptedQuantity = deliveryNoteItem.Quantity - returnedQuantity,
                RejectedQuantity = returnedQuantity,
                RejectReason = returnedQuantity > 0 ? item.RejectReason : null
            });
        }

        return (true, null, new DeliveryAcceptanceAppDto { Items = acceptanceItems });
    }
}
