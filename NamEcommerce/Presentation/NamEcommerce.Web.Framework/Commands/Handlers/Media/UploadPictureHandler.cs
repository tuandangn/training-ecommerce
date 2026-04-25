using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Media;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Commands.Models.Media;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Media;

public sealed class UploadPictureHandler : IRequestHandler<UploadPictureCommand, UploadPictureResultModel>
{
    private readonly IPictureAppService _pictureAppService;

    public UploadPictureHandler(IPictureAppService pictureAppService)
    {
        _pictureAppService = pictureAppService;
    }

    public async Task<UploadPictureResultModel> Handle(UploadPictureCommand request, CancellationToken cancellationToken)
    {
        var id = await _pictureAppService.CreatePictureAsync(new CreatePictureAppDto
        {
            Data = request.Data,
            MimeType = request.MimeType,
            FileName = request.FileName,
            Extension = request.Extension
        }).ConfigureAwait(false);

        var dataUrl = $"data:{request.MimeType};base64,{Convert.ToBase64String(request.Data)}";

        return new UploadPictureResultModel
        {
            Success = true,
            PictureId = id,
            DataUrl = dataUrl
        };
    }
}
