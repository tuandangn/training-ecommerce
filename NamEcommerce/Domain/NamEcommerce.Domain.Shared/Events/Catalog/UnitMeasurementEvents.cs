namespace NamEcommerce.Domain.Shared.Events.Catalog;

/// <summary>
/// Đơn vị đo vừa được tạo.
/// </summary>
public sealed record UnitMeasurementCreated(Guid UnitMeasurementId, string Name) : DomainEvent;

/// <summary>
/// Đơn vị đo được cập nhật (đổi tên hoặc DisplayOrder).
/// </summary>
public sealed record UnitMeasurementUpdated(Guid UnitMeasurementId) : DomainEvent;

/// <summary>
/// Đơn vị đo bị xoá (soft delete).
/// </summary>
public sealed record UnitMeasurementDeleted(Guid UnitMeasurementId, string Name) : DomainEvent;
