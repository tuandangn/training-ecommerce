namespace NamEcommerce.Domain.Shared.Events.Customers;

/// <summary>
/// Khách hàng vừa được tạo.
/// </summary>
public sealed record CustomerCreated(Guid CustomerId, string FullName, string PhoneNumber) : DomainEvent;

/// <summary>
/// Khách hàng được cập nhật thông tin.
/// </summary>
public sealed record CustomerUpdated(Guid CustomerId) : DomainEvent;

/// <summary>
/// Khách hàng bị xoá (soft delete).
/// </summary>
public sealed record CustomerDeleted(Guid CustomerId, string FullName) : DomainEvent;
