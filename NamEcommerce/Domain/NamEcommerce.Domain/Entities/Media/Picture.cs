using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Events.Media;

namespace NamEcommerce.Domain.Entities.Media;

[Serializable]
public sealed record Picture : AppAggregateEntity
{
    internal Picture(byte[] data, string mimeType) : base(Guid.NewGuid())
    {
        (Data, MimeType) = (data, mimeType);

        CreatedOnUtc = DateTime.UtcNow;
    }

    public byte[] Data { get; internal set; }
    public string MimeType { get; internal set; }

    public string? Extension { get; set; }
    public string? FileName { get; set; }

    public DateTime CreatedOnUtc { get; }

    /// <summary>
    /// Đánh dấu ảnh vừa được upload — Manager gọi trước <c>InsertAsync</c>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new PictureCreated(Id, MimeType));

    /// <summary>
    /// Đánh dấu ảnh bị xoá — Manager gọi trước <c>DeleteAsync</c>.
    /// </summary>
    internal void MarkDeleted()
        => RaiseDomainEvent(new PictureDeleted(Id));
}
