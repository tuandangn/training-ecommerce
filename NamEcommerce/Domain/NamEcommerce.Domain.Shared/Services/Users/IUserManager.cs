using NamEcommerce.Domain.Shared.Dtos.Users;

namespace NamEcommerce.Domain.Shared.Services.Users;

public interface IUserManager
{
    Task<bool> DoesUsernameExistAsync(string username, Guid? comparesWithCurrentId = null);

    Task<UserDto?> FindUserByUserNameAndPasswordAsync(string username, string password);

    Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto);
}
