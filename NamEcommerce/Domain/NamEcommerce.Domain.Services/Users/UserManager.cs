using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Users;

public sealed class UserManager : IUserManager
{
    private readonly IRepository<User> _userRepository;

    public UserManager(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> DoesUsernameExistAsync(string username, Guid? comparesWithCurrentId = null)
    {
        if (username is null)
            throw new ArgumentNullException(nameof(username));

        var categories = await _userRepository.GetAllAsync().ConfigureAwait(false);
        return categories.Any(category =>
            category.Username == username
            && (!comparesWithCurrentId.HasValue || comparesWithCurrentId == category.Id));
    }
}
