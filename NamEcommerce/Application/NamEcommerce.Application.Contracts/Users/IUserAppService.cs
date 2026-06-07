using NamEcommerce.Application.Contracts.Dtos.Users;

namespace NamEcommerce.Application.Contracts.Users;

public interface IUserAppService
{
    Task<bool> DoesUsernameExistsAsync(string username);

    Task<UserAppDto?> GetUserByIdAsync(Guid id);

    Task<IList<UserAppDto>> GetUsersAsync();

    Task<IList<UserAppDto>> GetUsersByRoleAsync(string roleName);

    Task<UserAppDto?> GetUserByUsernameAndPasswordAsync(string username, string password);

    Task<CreateUserResultAppDto> CreateUserAsync(CreateUserAppDto dto);

    Task<IList<RoleAppDto>> GetRolesAsync();

    Task EnsureSystemRolesAsync();

    Task<bool> HasUsersInRoleAsync(string roleName);

    Task<bool> IsUserInRoleAsync(Guid userId, string roleName);

    Task<UpdateUserRolesResultAppDto> UpdateUserRolesAsync(UpdateUserRolesAppDto dto);
}
