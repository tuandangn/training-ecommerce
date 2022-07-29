using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Media;

[Serializable]
public sealed record Picture : AppAggregateEntity
{
    public Picture(int id, byte[] data, string mimeType, string extension) : base(id)
        => (Data, MimeType, Extension) = (data, mimeType, extension);

    public byte[] Data { get; init; }
    public string MimeType { get; init; }
    public string Extension { get; init; }

    public string? SeoName { get; set; }
    public bool IsNew { get; init; }

    public DateTime CreatedOnUtc { get; init; }
    public DateTime UpdatedOnUtc { get; init; }
}
