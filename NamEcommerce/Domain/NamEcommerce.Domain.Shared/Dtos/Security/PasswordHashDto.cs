namespace NamEcommerce.Domain.Shared.Dtos.Security;

[Serializable]
public sealed record PasswordHashDto
{
    public required string PasswordHash { get; init; }
    public required string PasswordSalt { get; init; }

    public void Deconstruct(out string passwordHash, out string passwordSalt)
        => (passwordHash, passwordSalt) = (PasswordHash, PasswordSalt);
}
