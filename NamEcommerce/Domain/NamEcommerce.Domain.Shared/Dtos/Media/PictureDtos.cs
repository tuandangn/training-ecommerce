using NamEcommerce.Domain.Shared.Exceptions.Media;

namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public abstract record BasePictureDto
{
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
    public string? FileName { get; set; }
    public string? Extension { get; set; }

    public virtual void Verify()
    {
        if (Data is null || Data.Length == 0)
            throw new PictureDataIsInvalidException("Dữ liệu hình ảnh không được để trống");
        if (string.IsNullOrEmpty(MimeType))
            throw new PictureDataIsInvalidException("Loại file (MIME type) không được để trống");
    }
}

[Serializable]
public sealed record PictureDto(Guid Id) : BasePictureDto;

[Serializable]
public sealed record CreatePictureDto : BasePictureDto;
[Serializable]
public sealed record CreatePictureResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdatePictureDto(Guid Id) : BasePictureDto;
[Serializable]
public sealed record UpdatePictureResultDto(Guid Id) : BasePictureDto;

