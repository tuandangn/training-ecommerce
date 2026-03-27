using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Media;

[Serializable]
public sealed record Picture : AppAggregateEntity
{
    public Picture(byte[] data, string mimeType) : base(Guid.NewGuid())
    {
        (Data, MimeType) = (data, mimeType);

        CreatedOnUtc = DateTime.UtcNow;
    }

    public byte[] Data { get; internal set; }
    public string MimeType { get; internal set; }

    public string? Extension { get; set; }
    public string? FileName { get; set; }

    public DateTime CreatedOnUtc { get; }
}
