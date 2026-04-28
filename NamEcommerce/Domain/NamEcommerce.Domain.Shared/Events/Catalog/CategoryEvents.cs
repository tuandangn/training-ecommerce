namespace NamEcommerce.Domain.Shared.Events.Catalog;

/// <summary>
/// Danh mục vừa được tạo.
/// </summary>
public sealed record CategoryCreated(Guid CategoryId, string Name, Guid? ParentId) : DomainEvent;

/// <summary>
/// Danh mục được cập nhật (đổi tên, DisplayOrder hoặc ParentId).
/// </summary>
public sealed record CategoryUpdated(Guid CategoryId) : DomainEvent;

/// <summary>
/// Quan hệ cha-con của danh mục được cập nhật (ví dụ kéo thả trên cây).
/// </summary>
public sealed record CategoryParentChanged(Guid CategoryId, Guid? ParentId) : DomainEvent;

/// <summary>
/// Danh mục bị xoá (soft delete). Children categories đã được Manager bỏ ParentId trước đó.
/// </summary>
public sealed record CategoryDeleted(Guid CategoryId, string Name) : DomainEvent;
