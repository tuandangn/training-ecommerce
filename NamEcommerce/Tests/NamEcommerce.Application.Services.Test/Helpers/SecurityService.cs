using NamEcommerce.Domain.Shared.Dtos.Security;
using NamEcommerce.Domain.Shared.Services.Security;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class SecurityService
{
    public static Mock<ISecurityService> HashPassword(string password, PasswordHashDto @return)
    {
        var mock = new Mock<ISecurityService>();
        mock.Setup(s => s.HashPasswordAsync(password)).ReturnsAsync(@return).Verifiable();

        return mock;
    }
}
