using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Media;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Preparation;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Preparation;

public sealed class MarkOrderItemDeliveredHandler : IRequestHandler<MarkOrderItemDeliveredCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;
    private readonly IPictureAppService _pictureAppService;

    public MarkOrderItemDeliveredHandler(IOrderAppService orderAppService, IPictureAppService pictureAppService)
    {
        _orderAppService = orderAppService;
        _pictureAppService = pictureAppService;
    }

    public async Task<CommonResultModel> Handle(MarkOrderItemDeliveredCommand request, CancellationToken cancellationToken)
    {
        if (request.PictureData is null || request.PictureData.Length == 0)
        {
            return new CommonResultModel
            {
                Success = false,
                ErrorMessage = "Hình ảnh bằng chứng giao hàng là bắt buộc."
            };
        }

        var fileName = request.FileName;
        var extension = Path.GetExtension(fileName);
        var mimeType = request.ContentType;

        var pictureId = await _pictureAppService.CreatePictureAsync(new CreatePictureAppDto
        {
            Data = request.PictureData,
            FileName = fileName,
            Extension = extension,
            MimeType = mimeType
        }).ConfigureAwait(false);

        var result = await _orderAppService.MarkOrderItemDeliveredAsync(new MarkOrderItemDeliveredAppDto
        {
            OrderId = request.OrderId,
            OrderItemId = request.OrderItemId,
            PictureId = pictureId
        }).ConfigureAwait(false);

        return new CommonResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
