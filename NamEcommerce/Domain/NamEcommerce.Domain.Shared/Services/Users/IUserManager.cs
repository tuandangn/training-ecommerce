using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;

namespace NamEcommerce.Domain.Shared.Services.Users;

public interface IUserManager : IUsernameExistCheckingService
{
    Task<UserDto?> GetUserByIdAsync(Guid id);

    Task<IList<UserDto>> GetUsersAsync();

    Task<UserDto?> FindUserByUserNameAndPasswordAsync(string username, string password);

    Task<CreateUserResultDto> CreateUserAsync(CreateUserDto dto);

    Task<IList<RoleDto>> GetRolesAsync();

    Task<IList<string>> GetRoleNamesByUserIdAsync(Guid userId);

    Task<bool> HasUsersInRoleAsync(string roleName);

    Task<bool> IsUserInRoleAsync(Guid userId, string roleName);

    Task EnsureSystemRolesAsync();

    Task UpdateUserRolesAsync(UpdateUserRolesDto dto);

    Task LockUserAsync(Guid userId);

    Task UnlockUserAsync(Guid userId);

    Task DeleteUserAsync(Guid userId);

    Task ChangePasswordAsync(Guid userId, string newPassword);
}
