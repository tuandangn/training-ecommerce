namespace NamEcommerce.Domain.Shared.Events.Media;

/// <summary>
/// Ảnh vừa được upload (lưu vào storage).
/// </summary>
public sealed record PictureCreated(Guid PictureId, string MimeType) : DomainEvent;

/// <summary>
/// Ảnh bị xoá khỏi storage.
/// </summary>
public sealed record PictureDeleted(Guid PictureId) : DomainEvent;
