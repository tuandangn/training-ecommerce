namespace NamEcommerce.Application.Contracts.Dtos.Media;

[Serializable]
public sealed record CreatePictureAppDto
{
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
    public required string FileName { get; init; }
    public required string Extension { get; init; }
}
