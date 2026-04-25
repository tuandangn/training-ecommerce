using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.Media;

[Serializable]
public sealed class UploadPictureCommand : IRequest<UploadPictureResultModel>
{
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
    public required string FileName { get; init; }
    public required string Extension { get; init; }
}

[Serializable]
public sealed class UploadPictureResultModel
{
    public required bool Success { get; init; }
    public Guid? PictureId { get; init; }
    public string? DataUrl { get; init; }
    public string? ErrorMessage { get; init; }
}
