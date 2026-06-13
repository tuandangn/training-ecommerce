namespace NamEcommerce.Domain.Shared.Events.Users;

public sealed record UserCreated(Guid UserId, string Username, string FullName) : DomainEvent;

public sealed record UserUpdated(Guid UserId) : DomainEvent;

public sealed record UserPasswordChanged(Guid UserId) : DomainEvent;

public sealed record UserDeleted(Guid UserId, string Username) : DomainEvent;

public sealed record UserLocked(Guid UserId) : DomainEvent;

public sealed record UserUnlocked(Guid UserId) : DomainEvent;
