namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public sealed record FileInfoModel
{
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
    public string? Extension { get; init; }
    public string? FileName { get; init; }
}
