using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Services.Test.Helpers;
using NamEcommerce.Application.Services.Users;
using NamEcommerce.Domain.Shared.Dtos.Security;
using NamEcommerce.Domain.Shared.Dtos.Users;

namespace NamEcommerce.Application.Services.Test.Users;

public sealed class UserAppServiceTests
{
    #region DoesUsernameExistsAsync

    [Fact]
    public async Task DoesUsernameExistsAsync_UsernameIsNull_ThrowsArgumentNullException()
    {
        var userAppService = new UserAppService(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => userAppService.DoesUsernameExistsAsync(null!));
    }

    [Fact]
    public async Task DoesUsernameExistsAsync_UsernameIsFound_ReturnTrue()
    {
        var existsUsername = "exists-username";
        var userManagerMock = UserManager.SetUsernameExists(existsUsername);
        var userAppService = new UserAppService(userManagerMock.Object);

        var exists = await userAppService.DoesUsernameExistsAsync(existsUsername);

        Assert.True(exists);
        userManagerMock.Verify();
    }

    #endregion

    #region GetUserByUsernameAndPasswordAsync

    [Fact]
    public async Task GetUserByUsernameAndPasswordAsync_UsernameIsNull_ThrowsArgumentNullException()
    {
        var userAppService = new UserAppService(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => userAppService.GetUserByUsernameAndPasswordAsync(null!, "password"));
    }

    [Fact]
    public async Task GetUserByUsernameAndPasswordAsync_PasswordIsNull_ThrowsArgumentNullException()
    {
        var userAppService = new UserAppService(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => userAppService.GetUserByUsernameAndPasswordAsync("username", null!));
    }

    [Fact]
    public async Task GetUserByUsernameAndPasswordAsync_ReturnsFoundData()
    {
        var username = "username";
        var password = "password";
        var foundData = new NamEcommerce.Domain.Shared.Dtos.Users.UserDto(Guid.NewGuid())
        {
            Username = username,
            FullName = "full-name",
            PhoneNumber = "1234567890",
        };
        var userManagerMock = UserManager.FindByUsernameAndPasswordReturns(username, password, foundData);
        var userAppService = new UserAppService(userManagerMock.Object);

        var result = await userAppService.GetUserByUsernameAndPasswordAsync(username, password);

        Assert.Equal(foundData.Id, result!.Id);
        userManagerMock.Verify();
    }

    #endregion

    #region CreateUserAsync

    [Fact]
    public async Task CreateUserAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var userAppService = new UserAppService(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => userAppService.CreateUserAsync(null!));
    }

    [Fact]
    public async Task CreateUserAsync_DtoIsInvalid_ReturnsFalseResult()
    {
        var invalidDto = new CreateUserAppDto(null!, null!, null!, null!);
        var userAppService = new UserAppService(null!);

        var falseResult = await userAppService.CreateUserAsync(invalidDto);

        Assert.False(falseResult.Success);
        Assert.NotEmpty(falseResult.ErrorMessage!);
    }

    [Fact]
    public async Task CreateUserAsync_UsernameExists_ReturnsFalseResult()
    {
        var existingUsername = "existing-username";
        var usernameExistDto = new CreateUserAppDto(existingUsername, "password", "name", "phoneNumber");
        var userManagerMock = UserManager.SetUsernameExists(existingUsername);
        var userAppService = new UserAppService(userManagerMock.Object);

        var falseResult = await userAppService.CreateUserAsync(usernameExistDto);

        Assert.False(falseResult.Success);
        userManagerMock.Verify();
    }

    [Fact]
    public async Task CreateUserAsync_UsernameNotExists_ReturnsResult()
    {
        var createUserDto = new CreateUserAppDto("username", "password", "fullName", "phoneNumber");
        var createdUserId = Guid.NewGuid();
        var userManagerMock = UserManager.SetUsernameExists(createUserDto.Username, false)
            .CreateUserReturns(
            new CreateUserDto
            {
                Username = createUserDto.Username,
                Password = "password",
                FullName = createUserDto.FullName,
                PhoneNumber = createUserDto.PhoneNumber,
                Address = createUserDto.Address
            },
            new Domain.Shared.Dtos.Users.CreateUserResultDto
            {
                CreatedId = createdUserId
            });
        var userAppService = new UserAppService(userManagerMock.Object);

        var createdResult = await userAppService.CreateUserAsync(createUserDto);

        Assert.True(createdResult.Success);
        Assert.Equal(createdUserId, createdResult.CreatedId);
        userManagerMock.Verify();
    }

    #endregion
}
