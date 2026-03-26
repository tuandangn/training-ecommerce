using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Users;

public sealed class UserManager : IUserManager
{
    private readonly IRepository<User> _userRepository;
    private readonly IEntityDataReader<User> _userEntityDataReader;
    private readonly ISecurityService _securityService;

    public UserManager(IRepository<User> userRepository, IEntityDataReader<User> userEntityDataReader, ISecurityService securityService)
    {
        _userRepository = userRepository;
        _userEntityDataReader = userEntityDataReader;
        _securityService = securityService;
    }

    public async Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        dto.Verify();

        if (await DoesUsernameExistAsync(dto.Username).ConfigureAwait(false))
            throw new UsernameExistsException(dto.Username);

        var user = new User(Guid.NewGuid(), dto.Username, dto.PasswordHash, dto.PasswordSalt, dto.FullName, dto.PhoneNumber)
        {
            Address = dto.Address,
            CreatedOnUtc = DateTime.UtcNow
        };
        user = await _userRepository.InsertAsync(user).ConfigureAwait(false);

        return new CreateUserResultDto
        {
            CreatedId = user.Id
        };
    }

    public async Task<bool> DoesUsernameExistAsync(string username, Guid? comparesWithCurrentId = null)
    {
        if (username is null)
            throw new ArgumentNullException(nameof(username));

        var categories = await _userEntityDataReader.GetAllAsync().ConfigureAwait(false);
        return categories.Any(category =>
            category.Username == username
            && (!comparesWithCurrentId.HasValue || comparesWithCurrentId != category.Id));
    }

    public async Task<UserDto?> FindUserByUserNameAndPasswordAsync(string username, string password)
    {
        if (username is null)
            throw new ArgumentNullException(nameof(username));

        if (password is null)
            throw new ArgumentNullException(nameof(password));

        var query = from user in _userEntityDataReader.DataSource
                    where user.Username == username
                    select user;
        var users = query.ToList();
        foreach (var user in users)
        {
            var valid = await _securityService.VerifyPasswordAsync(password, user.PasswordHash, user.PasswordSalt).ConfigureAwait(false);
            if (valid) return user.ToDto();
        }

        return null;
    }
}
