using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Dtos.Security;
using NamEcommerce.Domain.Shared.Services.Security;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class SecurityService
{
    public static Mock<ISecurityService> WhenHashReturns(string password, (string passwordHash, string passwordSalt) @return)
    {
        var mock = new Mock<ISecurityService>();
        mock.WhenHashReturns(password, @return);
        return mock;
    }
    public static Mock<ISecurityService> WhenHashReturns(this Mock<ISecurityService> mock, string password, (string passwordHash, string passwordSalt) @return)
    {
        mock.Setup(s => s.HashPasswordAsync(password)).ReturnsAsync(new PasswordHashDto
        {
            PasswordHash = @return.passwordHash,
            PasswordSalt = @return.passwordSalt
        }).Verifiable();
        return mock;
    }
    public static Mock<ISecurityService> VerifyFalse(string password, string salt, string passwordHash)
    {
        var mock = new Mock<ISecurityService>();
        mock.Setup(s => s.VerifyPasswordAsync(password, passwordHash, salt)).ReturnsAsync(false).Verifiable();
        return mock;
    }
    public static Mock<ISecurityService> VerifyFalse(this Mock<ISecurityService> mock, string password, string salt, string passwordHash)
    {
        mock.Setup(s => s.VerifyPasswordAsync(password, passwordHash, salt)).ReturnsAsync(false).Verifiable();
        return mock;
    }
    public static Mock<ISecurityService> VerifyTrue(string password, string passwordHash, string salt)
    {
        var mock = new Mock<ISecurityService>();
        mock.Setup(s => s.VerifyPasswordAsync(password, passwordHash, salt)).ReturnsAsync(true).Verifiable();
        return mock;
    }
}
