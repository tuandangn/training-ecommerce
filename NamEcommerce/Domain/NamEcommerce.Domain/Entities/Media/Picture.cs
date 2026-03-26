using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Media;

[Serializable]
public sealed record Picture : AppAggregateEntity
{
    public Picture(Guid id, byte[] data, string mimeType) : base(id)
        => (Data, MimeType) = (data, mimeType);

    public byte[] Data { get; internal set; }
    public string MimeType { get; internal set; }

    public string? Extension { get; set; }
    public string? FileName { get; set; }

    public DateTime CreatedOnUtc { get; init; }
        = DateTime.UtcNow;
    public DateTime? UpdatedOnUtc { get; init; }
}
