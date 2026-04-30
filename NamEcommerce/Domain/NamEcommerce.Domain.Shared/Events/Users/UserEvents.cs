namespace NamEcommerce.Domain.Shared.Events.Users;

/// <summary>
/// Tài khoản người dùng vừa được tạo.
/// </summary>
public sealed record UserCreated(Guid UserId, string Username, string FullName) : DomainEvent;

/// <summary>
/// Thông tin tài khoản (FullName, Address, PhoneNumber...) được cập nhật.
/// </summary>
public sealed record UserUpdated(Guid UserId) : DomainEvent;

/// <summary>
/// Mật khẩu tài khoản vừa được đổi (qua reset hoặc người dùng tự đổi).
/// </summary>
public sealed record UserPasswordChanged(Guid UserId) : DomainEvent;

/// <summary>
/// Tài khoản bị xoá (soft delete).
/// </summary>
public sealed record UserDeleted(Guid UserId, string Username) : DomainEvent;
