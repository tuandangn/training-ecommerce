using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Services.Security;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.Users;

public sealed class UserManager : IUserManager
{
    private readonly IRepository<User> _userRepository;
    private readonly IEntityDataReader<User> _userEntityDataReader;
    private readonly ISecurityService _securityService;
    private readonly IEventPublisher _eventPublisher;

    public UserManager(IRepository<User> userRepository, IEntityDataReader<User> userEntityDataReader, ISecurityService securityService, IEventPublisher eventPublisher)
    {
        _userRepository = userRepository;
        _userEntityDataReader = userEntityDataReader;
        _securityService = securityService;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        dto.Verify();

        if (await DoesUsernameExistAsync(dto.Username).ConfigureAwait(false))
            throw new UsernameExistsException(dto.Username);

        var user = new User(dto.Username, dto.FullName, dto.PhoneNumber)
        {
            Address = dto.Address
        };
        await user.SetPasswordAsync(dto.Password, _securityService).ConfigureAwait(false);
        user = await _userRepository.InsertAsync(user).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(user).ConfigureAwait(false);

        return new CreateUserResultDto
        {
            CreatedId = user.Id
        };
    }

    public async Task<bool> DoesUsernameExistAsync(string username, Guid? comparesWithCurrentId = null)
    {
        if (username is null)
            throw new ArgumentNullException(nameof(username));

        var users = await _userEntityDataReader.GetAllAsync().ConfigureAwait(false);
        return users.Any(user =>
            user.Username == username
            && (!comparesWithCurrentId.HasValue || comparesWithCurrentId != user.Id));
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
