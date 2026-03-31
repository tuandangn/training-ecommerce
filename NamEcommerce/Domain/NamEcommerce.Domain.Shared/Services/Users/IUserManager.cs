using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;

namespace NamEcommerce.Domain.Shared.Services.Users;

public interface IUserManager : IUsernameExistCheckingService
{
    Task<UserDto?> FindUserByUserNameAndPasswordAsync(string username, string password);

    Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto);
}
