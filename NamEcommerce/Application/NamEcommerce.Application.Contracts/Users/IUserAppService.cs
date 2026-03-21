using NamEcommerce.Application.Contracts.Dtos.Users;

namespace NamEcommerce.Application.Contracts.Users;

public interface IUserAppService
{
    Task<bool> DoesUsernameExistsAsync(string username);

    Task<UserAppDto?> GetUserByUsernameAndPasswordAsync(string username, string password);

    Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto);
}
