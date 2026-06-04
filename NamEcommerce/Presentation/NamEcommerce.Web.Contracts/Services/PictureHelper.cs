namespace NamEcommerce.Web.Contracts.Services;

public static class PictureHelper
{
    public static string GetPictureUrl(Guid? pictureId) => pictureId.HasValue ? $"/Picture/{pictureId.Value}" : string.Empty;
}
