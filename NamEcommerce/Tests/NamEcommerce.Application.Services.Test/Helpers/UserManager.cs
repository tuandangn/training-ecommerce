using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.TestHelper;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class UserManager
{
    public static Mock<IUserManager> SetUsernameExists(string username)
    {
        var mock = new Mock<IUserManager>();
        mock.Setup(r => r.DoesUsernameExistAsync(username, null)).ReturnsAsync(true).Verifiable();

        return mock;
    }
    public static Mock<IUserManager> SetUsernameExists(string username, bool exists)
    {
        var mock = new Mock<IUserManager>();
        mock.Setup(r => r.DoesUsernameExistAsync(username, null)).ReturnsAsync(exists).Verifiable();

        return mock;
    }

    public static Mock<IUserManager> FindByUsernameAndPasswordReturns(string username, string password, UserDto dto)
    {
        var mock = new Mock<IUserManager>();
        mock.Setup(r => r.FindUserByUserNameAndPasswordAsync(username, password)).ReturnsAsync(dto).Verifiable();

        return mock;
    }


    public static Mock<IUserManager> CreateUserReturns(this Mock<IUserManager> mock, CreateUserDto dto, CreateUserResultDto @return)
    {
        mock.Setup(r => r.CreateUserAsync(dto)).ReturnsAsync(@return);

        return mock;
    }
}
