namespace NamEcommerce.Application.Contracts.Dtos.Media;

[Serializable]
public sealed record Base64PictureAppDto
{
    public required string Base64Value { get; set; }
    public required string MimeType { get; set; }
    public string? FileName { get; set; }
    public string? Extension { get; set; }
}
