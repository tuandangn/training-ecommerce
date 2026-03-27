using NamEcommerce.Domain.Shared.Dtos.Security;
using NamEcommerce.Domain.Shared.Services.Security;

namespace NamEcommerce.Domain.Services.Security;

public sealed class SecurityService : ISecurityService
{
    public Task<PasswordHashDto> HashPasswordAsync(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));

        var passwordSalt = BCrypt.Net.BCrypt.GenerateSalt();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, passwordSalt);
        if (string.IsNullOrEmpty(passwordHash))
            throw new InvalidOperationException("Failed to hash the password.");

        return Task.FromResult(new PasswordHashDto
        {
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        });
    }

    public Task<bool> VerifyPasswordAsync(string password, string passwordHash, string salt)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(nameof(password));
        if (string.IsNullOrEmpty(salt))
            throw new ArgumentNullException(nameof(salt));
        if (string.IsNullOrEmpty(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash));

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);
        if (string.IsNullOrEmpty(hashedPassword))
            throw new InvalidOperationException("Failed to hash the password.");

        var isValid = string.Equals(hashedPassword, passwordHash);

        return Task.FromResult(isValid);
    }
}
