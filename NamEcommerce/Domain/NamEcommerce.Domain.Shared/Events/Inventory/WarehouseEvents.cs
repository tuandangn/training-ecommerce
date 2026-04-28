namespace NamEcommerce.Domain.Shared.Events.Inventory;

/// <summary>
/// Kho vừa được tạo.
/// </summary>
public sealed record WarehouseCreated(Guid WarehouseId, string Code, string Name) : DomainEvent;

/// <summary>
/// Kho được cập nhật thông tin (code, name, address, type, active...).
/// </summary>
public sealed record WarehouseUpdated(Guid WarehouseId) : DomainEvent;

/// <summary>
/// Kho bị xoá (soft delete).
/// </summary>
public sealed record WarehouseDeleted(Guid WarehouseId, string Code, string Name) : DomainEvent;
