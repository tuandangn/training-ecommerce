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
        var result = await deliveryNoteAppService.MarkDeliveredAsync(new MarkDeliveryNoteDeliveredAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            PictureIds = [request.PictureId],
            ReceiverName = request.ReceiverName,
            CompletionMetadata = new DeliveryCompletionMetadataAppDto
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                LocationAddress = request.LocationAddress,
                Note = request.Note,
                Source = "MobilePwa",
                IdempotencyKey = request.IdempotencyKey,
                CashCollectedAmount = request.CashCollectedAmount
            }
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            SuccessMessage = result.Success ? "Msg.SaveSuccess" : null
        };
    }
}
