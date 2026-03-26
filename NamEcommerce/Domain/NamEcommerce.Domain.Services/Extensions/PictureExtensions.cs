using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Dtos.Catalog;

namespace NamEcommerce.Domain.Services.Extensions;

public static class PictureExtensions
{
    public static PictureDto ToDto(this Picture picture)
        => new PictureDto(picture.Id)
        {
            Data = picture.Data,
            MimeType = picture.MimeType,
            Extension = picture.Extension,
            FileName = picture.FileName
        };
}
