namespace NamEcommerce.Web.Contracts.Models.Media;

[Serializable]
public sealed class PictureFileModel
{
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
    public string? FileName { get; init; }
}
