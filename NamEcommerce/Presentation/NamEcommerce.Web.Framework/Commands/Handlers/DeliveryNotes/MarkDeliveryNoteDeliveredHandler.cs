using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class MarkDeliveryNoteDeliveredHandler(IDeliveryNoteAppService deliveryNoteAppService)
    : IRequestHandler<MarkDeliveryNoteDeliveredCommand, MarkDeliveryNoteDeliveredResult>
{
    public async Task<MarkDeliveryNoteDeliveredResult> Handle(MarkDeliveryNoteDeliveredCommand request, CancellationToken cancellationToken)
    {
        if (request.PictureIds.Count == 0)
            return new MarkDeliveryNoteDeliveredResult(false, "Vui lòng tải lên ít nhất 1 hình ảnh bằng chứng giao hàng.");

        var appResult = await deliveryNoteAppService.MarkDeliveredAsync(new MarkDeliveryNoteDeliveredAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            PictureIds = request.PictureIds.ToList().AsReadOnly(),
            ReceiverName = request.ReceiverName,
            CompletionMetadata = new DeliveryCompletionMetadataAppDto
            {
                Source = request.Source ?? "Admin",
                CashCollectedAmount = request.CashCollectedAmount ?? 0
            },
            Acceptance = new DeliveryAcceptanceAppDto
            {
                AgreedCustomerCharge = request.AgreedCustomerCharge,
                AgreedCustomerChargeReason = request.AgreedCustomerChargeReason,
                CompensateInNextDelivery = request.CompensateInNextDelivery,
                Items = request.Items.Select(item => new DeliveryAcceptanceItemAppDto
                {
                    DeliveryNoteItemId = item.DeliveryNoteItemId,
                    AcceptedQuantity = item.AcceptedQuantity,
                    RejectedQuantity = item.RejectedQuantity,
                    RejectReason = item.RejectReason
                }).ToList()
            }
        }).ConfigureAwait(false);

        return new MarkDeliveryNoteDeliveredResult(appResult.Success, appResult.ErrorMessage);
    }
}
