using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class SecurityService
{
    public static Mock<ISecurityService> VerifyFalse(string password, string salt, string passwordHash)
    {
        var mock = new Mock<ISecurityService>();
        mock.Setup(s => s.VerifyPasswordAsync(password, passwordHash, salt)).ReturnsAsync(false).Verifiable();
        return mock;
    }
    public static Mock<ISecurityService> VerifyTrue(string password, string salt, string passwordHash)
    {
        var mock = new Mock<ISecurityService>();
        mock.Setup(s => s.VerifyPasswordAsync(password, passwordHash, salt)).ReturnsAsync(true).Verifiable();
        return mock;
    }
}
