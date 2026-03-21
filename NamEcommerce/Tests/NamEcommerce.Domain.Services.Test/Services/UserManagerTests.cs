using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Services.Users;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Exceptions.Users;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class UserManagerTests
{
    #region DoesUsernameExistAsync

    [Fact]
    public async Task DoesUsernameExistAsync_UsernameIsNull_ThrowsArgumentNullException()
    {
        var userManager = new UserManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => userManager.DoesUsernameExistAsync(null!));
    }

    [Fact]
    public async Task DoesUsernameExistAsync_UsernameIsMatchAndCompareIdIsNotProvided_ReturnsTrue()
    {
        var testUsername = "test-username-existing";
        var userDataReaderMock = UserDataReader.SetUsernameExists(testUsername, Guid.NewGuid());
        var userManager = new UserManager(null!, userDataReaderMock.Object, null!);

        var usernameExists = await userManager.DoesUsernameExistAsync(testUsername, comparesWithCurrentId: null);

        Assert.True(usernameExists);
        userDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesUsernameExistAsync_UsernameIsMatchAndCompareIdEquals_ReturnsFalse()
    {
        var hasNameUserId = Guid.NewGuid();
        var testUsername = "test-username-existing";
        var userDataReaderMock = UserDataReader.SetUsernameExists(testUsername, hasNameUserId);
        var userManager = new UserManager(null!, userDataReaderMock.Object, null!);

        var usernameExists = await userManager.DoesUsernameExistAsync(testUsername, comparesWithCurrentId: hasNameUserId);

        Assert.False(usernameExists);
        userDataReaderMock.Verify();
    }

    #endregion

    #region FindUserByUserNameAndPasswordAsync

    [Fact]
    public async Task FindUserByUserNameAndPasswordAsync_UsernameIsNull_ThrowsArgumentNullException()
    {
        var userManager = new UserManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => userManager.FindUserByUserNameAndPasswordAsync(null!, "password"));
    }

    [Fact]
    public async Task FindUserByUserNameAndPasswordAsync_PasswordIsNull_ThrowsArgumentNullException()
    {
        var userManager = new UserManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => userManager.FindUserByUserNameAndPasswordAsync("username", null!));
    }

    [Fact]
    public async Task FindUserByUserNameAndPasswordAsync_UsernameNotFound_ReturnsNull()
    {
        var notFoundUserName = "not-found-username";
        var userDataReaderMock = UserDataReader.Empty();
        var userManager = new UserManager(null!, userDataReaderMock.Object, null!);

        var result = await userManager.FindUserByUserNameAndPasswordAsync(notFoundUserName, "password");

        Assert.Null(result);
        userDataReaderMock.Verify();
    }

    [Fact]
    public async Task FindUserByUserNameAndPasswordAsync_PasswordHashMismatch_ReturnsNull()
    {
        var username = "username";
        var password = "not-match-password";
        var user = new User(Guid.NewGuid(), username, "passwordHash", "passwordSalt", "fullName", "phoneNumber");
        var userDataReaderMock = UserDataReader.HasOne(user);
        var securityServiceMock = SecurityService.VerifyFalse(password, user.PasswordSalt, user.PasswordHash);
        var userManager = new UserManager(null!, userDataReaderMock.Object, securityServiceMock.Object);

        var result = await userManager.FindUserByUserNameAndPasswordAsync(username, password);

        Assert.Null(result);
        userDataReaderMock.Verify();
    }

    [Fact]
    public async Task FindUserByUserNameAndPasswordAsync_PasswordHashIsMatch_ReturnsDto()
    {
        var username = "username";
        var password = "match-password";
        var user = new User(Guid.NewGuid(), username, "passwordHash", "passwordSalt", "fullName", "phoneNumber");
        var userDataReaderMock = UserDataReader.HasOne(user);
        var securityServiceMock = SecurityService.VerifyTrue(password, user.PasswordSalt, user.PasswordHash);
        var userManager = new UserManager(null!, userDataReaderMock.Object, securityServiceMock.Object);

        var result = await userManager.FindUserByUserNameAndPasswordAsync(username, password);

        Assert.Equal(user.ToDto(), result);
        userDataReaderMock.Verify();
    }

    #endregion

    #region CreateUserAsync

    [Fact]
    public async Task CreateUserAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var userManager = new UserManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => userManager.CreateUserAsync(null!));
    }

    [Fact]
    public async Task CreateUserAsync_DataIsInvalid_ThrowsArgumentException()
    {
        var invalidCreateUserDto = new CreateUserDto
        {
            Username = null!,
            FullName = null!,
            PasswordHash = null!,
            PasswordSalt = null!,
            PhoneNumber = null!
        };
        var userManager = new UserManager(null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentException>(() => userManager.CreateUserAsync(invalidCreateUserDto));
    }

    [Fact]
    public async Task CreateUserAsync_UsernameIsExists_ThrowsUsernameExistsException()
    {
        var existsUsernameUserDto = new CreateUserDto
        {
            Username = "exists-username",
            FullName = "name",
            PhoneNumber = "phone",
            PasswordHash = "hash",
            PasswordSalt = "salt",
        };
        var userDataReaderMock = UserDataReader.SetUsernameExists(existsUsernameUserDto.Username, Guid.NewGuid());
        var userManager = new UserManager(null!, userDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<UsernameExistsException>(() => userManager.CreateUserAsync(existsUsernameUserDto));
        userDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateUserAsync_UsernameIsNotExists_ReturnsCreatedUserDto()
    {
        var usernameUserDto = new CreateUserDto
        {
            Username = "username",
            FullName = "name",
            PhoneNumber = "phone",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Address = "address"
        };
        var user = new User(Guid.NewGuid(), usernameUserDto.Username,
            usernameUserDto.PasswordHash, usernameUserDto.PasswordSalt,
            usernameUserDto.FullName, usernameUserDto.PhoneNumber)
        {
            Address = usernameUserDto.Address
        };
        var userRepositoryMock = UserRepository.CreateUserWillReturns(user, user);
        var userDataReaderStub = UserDataReader.Empty();
        var userManager = new UserManager(userRepositoryMock.Object, userDataReaderStub.Object, null!);

        var result = await userManager.CreateUserAsync(usernameUserDto);

        Assert.Equal(user.Id, result.CreatedId);
        userRepositoryMock.Verify();
    }

    #endregion
}
