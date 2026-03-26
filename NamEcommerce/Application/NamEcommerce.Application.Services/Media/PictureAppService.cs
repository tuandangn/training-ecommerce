using NamEcommerce.Application.Contracts.Dtos.Media;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Domain.Shared.Services.Media;

namespace NamEcommerce.Application.Services.Media;

public sealed class PictureAppService : IPictureAppService
{
    private readonly IPictureManager _pictureManager;

    public PictureAppService(IPictureManager pictureManager)
    {
        _pictureManager = pictureManager;
    }

    public async Task<Base64PictureAppDto?> GetBase64PictureByIdAsync(Guid id)
    {
        var picture = await _pictureManager.GetPictureByIdAsync(id).ConfigureAwait(false);
        if (picture is null)
            return null;

        var base64 = Convert.ToBase64String(picture.Data);
        if (!string.IsNullOrEmpty(picture.MimeType))
            base64 = $"data:{picture.MimeType};base64,{base64}";

        return new Base64PictureAppDto
        {
            Base64Value = base64,
            MimeType = picture.MimeType,
            Extension = picture.Extension,
            FileName = picture.FileName
        };
    }
}
