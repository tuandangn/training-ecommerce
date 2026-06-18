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
        if (request.Items.Count == 0 || request.Items.All(i => i.ReturnedQuantity <= 0))
            return (true, null, null);

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(request.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return (false, "Error.DeliveryNoteNotFound", null);

        if (request.Items.Any(item => item.ProductId == Guid.Empty))
            return (false, "Error.DeliveryAcceptance.InvalidItem", null);

        var itemsByProductId = deliveryNote.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var productRequests = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                ReturnedQuantity = group.Sum(item => Math.Max(0m, item.ReturnedQuantity)),
                RejectReason = group.FirstOrDefault(item => item.ReturnedQuantity > 0 && !string.IsNullOrWhiteSpace(item.RejectReason))?.RejectReason
            })
            .ToList();

        var acceptanceItems = new List<DeliveryAcceptanceItemAppDto>();
        foreach (var item in productRequests)
        {
            if (!itemsByProductId.TryGetValue(item.ProductId, out var deliveryNoteItems))
                return (false, "Error.DeliveryAcceptance.InvalidItem", null);

            var totalQuantity = deliveryNoteItems.Sum(deliveryNoteItem => deliveryNoteItem.Quantity);
            if (item.ReturnedQuantity - totalQuantity > 0.0001m)
                return (false, "Error.DeliveryAcceptance.QuantityMismatch", null);

            var remainingReturnedQuantity = item.ReturnedQuantity;
            foreach (var deliveryNoteItem in deliveryNoteItems)
            {
                var returnedQuantity = Math.Min(deliveryNoteItem.Quantity, remainingReturnedQuantity);
                remainingReturnedQuantity -= returnedQuantity;
                acceptanceItems.Add(new DeliveryAcceptanceItemAppDto
                {
                    DeliveryNoteItemId = deliveryNoteItem.Id,
                    AcceptedQuantity = deliveryNoteItem.Quantity - returnedQuantity,
                    RejectedQuantity = returnedQuantity,
                    RejectReason = returnedQuantity > 0 ? item.RejectReason : null
                });
            }
        }

        return (true, null, new DeliveryAcceptanceAppDto { Items = acceptanceItems });
    }
}
