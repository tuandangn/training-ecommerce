namespace NamEcommerce.Domain.Shared.Events.Catalog;

/// <summary>
/// Sản phẩm vừa được tạo.
/// </summary>
public sealed record ProductCreated(Guid ProductId, string Name) : DomainEvent;

/// <summary>
/// Sản phẩm được cập nhật. <paramref name="DeletedPictureIds"/> là danh sách
/// PictureId không còn liên kết với product nữa và cần được dọn dẹp khỏi storage.
/// </summary>
public sealed record ProductUpdated(Guid ProductId, IReadOnlyCollection<Guid> DeletedPictureIds) : DomainEvent;

/// <summary>
/// Sản phẩm bị xoá. <paramref name="PictureIds"/> là toàn bộ ảnh thuộc product
/// tại thời điểm xoá — handler sẽ dọn các ảnh này khỏi storage.
/// </summary>
public sealed record ProductDeleted(Guid ProductId, string Name, IReadOnlyCollection<Guid> PictureIds) : DomainEvent;

/// <summary>
/// Giá bán hoặc giá vốn của sản phẩm thay đổi. Phục vụ ghi history + cập nhật phụ thuộc.
/// </summary>
public sealed record ProductPriceChanged(
    Guid ProductId,
    decimal OldUnitPrice,
    decimal NewUnitPrice,
    decimal OldCostPrice,
    decimal NewCostPrice) : DomainEvent;
