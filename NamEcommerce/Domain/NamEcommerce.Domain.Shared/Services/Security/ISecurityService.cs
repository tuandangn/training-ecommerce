using NamEcommerce.Domain.Shared.Dtos.Security;

namespace NamEcommerce.Domain.Shared.Services.Security;

public interface ISecurityService
{
    Task<PasswordHashDto> HashPasswordAsync(string password);

    Task<bool> VerifyPasswordAsync(string password, string hashedPassword, string salt);
}
