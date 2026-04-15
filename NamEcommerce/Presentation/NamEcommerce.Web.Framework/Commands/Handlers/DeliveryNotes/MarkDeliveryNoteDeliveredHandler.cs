using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Media;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class MarkDeliveryNoteDeliveredHandler : IRequestHandler<MarkDeliveryNoteDeliveredCommand, MarkDeliveryNoteDeliveredResult>
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;
    private readonly IPictureAppService _pictureAppService;

    public MarkDeliveryNoteDeliveredHandler(
        IDeliveryNoteAppService deliveryNoteAppService,
        IPictureAppService pictureAppService)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
        _pictureAppService = pictureAppService;
    }

    public async Task<MarkDeliveryNoteDeliveredResult> Handle(MarkDeliveryNoteDeliveredCommand request, CancellationToken cancellationToken)
    {
        if (request.PictureData == null || request.PictureData.Length == 0)
        {
            return new MarkDeliveryNoteDeliveredResult(false, "Vui lòng chụp ảnh bằng chứng giao hàng.");
        }

        try
        {
            // 1. Save picture
            var pictureId = await _pictureAppService.CreatePictureAsync(new CreatePictureAppDto
            {
                Data = request.PictureData,
                MimeType = request.PictureContentType ?? "image/jpeg",
                Extension = ".jpg",
                FileName = request.PictureFileName ?? "delivery_proof.jpg"
            }).ConfigureAwait(false);

            // 2. Mark delivered
            var appResult = await _deliveryNoteAppService.MarkDeliveredAsync(new MarkDeliveryNoteDeliveredAppDto
            {
                DeliveryNoteId = request.DeliveryNoteId,
                PictureId = pictureId,
                ReceiverName = request.ReceiverName
            }).ConfigureAwait(false);
            
            return new MarkDeliveryNoteDeliveredResult(appResult.Success, appResult.ErrorMessage);
        }
        catch (Exception ex)
        {
            return new MarkDeliveryNoteDeliveredResult(false, ex.Message);
        }
    }
}
