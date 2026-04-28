namespace NamEcommerce.Domain.Shared.Events.Catalog;

/// <summary>
/// Nhà cung cấp vừa được tạo.
/// </summary>
public sealed record VendorCreated(Guid VendorId, string Name, string PhoneNumber) : DomainEvent;

/// <summary>
/// Nhà cung cấp được cập nhật (đổi tên, SĐT, địa chỉ hoặc DisplayOrder).
/// </summary>
public sealed record VendorUpdated(Guid VendorId) : DomainEvent;

/// <summary>
/// Nhà cung cấp bị xoá (soft delete).
/// </summary>
public sealed record VendorDeleted(Guid VendorId, string Name) : DomainEvent;
