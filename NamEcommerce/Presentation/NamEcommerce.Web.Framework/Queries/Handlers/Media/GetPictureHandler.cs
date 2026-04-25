using MediatR;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Web.Contracts.Models.Media;
using NamEcommerce.Web.Contracts.Queries.Models.Media;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Media;

public sealed class GetPictureHandler : IRequestHandler<GetPictureQuery, PictureFileModel?>
{
    private readonly IPictureAppService _pictureAppService;

    public GetPictureHandler(IPictureAppService pictureAppService)
    {
        _pictureAppService = pictureAppService;
    }

    public async Task<PictureFileModel?> Handle(GetPictureQuery request, CancellationToken cancellationToken)
    {
        var dto = await _pictureAppService.GetBase64PictureByIdAsync(request.Id).ConfigureAwait(false);
        if (dto is null)
            return null;

        // Decode base64 back to binary (strip "data:image/...;base64," prefix if present)
        var base64 = dto.Base64Value;
        var commaIdx = base64.IndexOf(',');
        if (commaIdx >= 0) base64 = base64[(commaIdx + 1)..];

        return new PictureFileModel
        {
            Data = Convert.FromBase64String(base64),
            MimeType = dto.MimeType,
            FileName = dto.FileName
        };
    }
}
