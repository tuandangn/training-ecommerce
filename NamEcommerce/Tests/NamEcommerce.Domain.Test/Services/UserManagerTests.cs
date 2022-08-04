using NamEcommerce.Domain.Services.Users;

namespace NamEcommerce.Domain.Test.Services;

public sealed class UserManagerTests
{
    #region DoesUsernameExistAsync

    [Fact]
    public async Task DoesUsernameExistAsync_UsernameIsNull_ThrowsArgumentNullException()
    {
        var userManager = new UserManager(null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => userManager.DoesUsernameExistAsync(null!));
    }

    [Fact]
    public async Task DoesUsernameExistAsync_UsernameIsMatchAndCompareIdIsNotProvided_ReturnsTrue()
    {
        var testUsername = "test-username-existing";
        var userRepositoryMock = UserRepository.SetUsernameExists(testUsername, Guid.NewGuid());
        var userManager = new UserManager(userRepositoryMock.Object);

        var usernameExists = await userManager.DoesUsernameExistAsync(testUsername, comparesWithCurrentId: null);

        Assert.True(usernameExists);
        userRepositoryMock.Verify();
    }

    [Fact]
    public async Task DoesUsernameExistAsync_UsernameIsMatchAndCompareIdEquals_ReturnsFalse()
    {
        var hasNameUserId = Guid.NewGuid();
        var testUsername = "test-username-existing";
        var userRepositoryMock = UserRepository.SetUsernameExists(testUsername, hasNameUserId);
        var userManager = new UserManager(userRepositoryMock.Object);

        var usernameExists = await userManager.DoesUsernameExistAsync(testUsername, comparesWithCurrentId: hasNameUserId);

        Assert.False(usernameExists);
        userRepositoryMock.Verify();
    }

    #endregion
}
