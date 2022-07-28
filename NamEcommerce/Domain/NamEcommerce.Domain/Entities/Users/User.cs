using System;

namespace NamEcommerce.Domain.Entities.Users;

[Serializable]
public sealed record User : AppEntity
{
    internal User(int id, string username, string fullName, string passwordHash) : base(id)
        => (Username, FullName, PasswordHash) = (username, fullName, passwordHash);

    public string Username { get; init; }

    public string FullName { get; init; }

    public string PasswordHash { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
