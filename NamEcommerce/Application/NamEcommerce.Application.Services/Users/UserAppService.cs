using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Users;

public sealed class UserAppService : IUserAppService
{
    private readonly IUserManager _userManager;

    public UserAppService(IUserManager userManager)
    {
        _userManager = userManager;
    }

    public async Task<CreateUserResultAppDto> CreateUserAsync(CreateUserAppDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var (valid, errorMessage) = CreateUserAppDto.Validate(dto);
        if (!valid)
        {
            return new CreateUserResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (await _userManager.DoesUsernameExistAsync(dto.Username).ConfigureAwait(false))
        {
            return new CreateUserResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.UsernameAlreadyExists"
            };
        }
        var user = await _userManager.CreateUserAsync(
            new CreateUserDto
            {
                Username = dto.Username,
                Password = dto.Password,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address
            });

        return new CreateUserResultAppDto
        {
            Success = user != null,
            CreatedId = user?.CreatedId,
            ErrorMessage = user == null ? "Error.UserCreateFailed" : null
        };
    }

    public async Task<bool> DoesUsernameExistsAsync(string username)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentNullException(nameof(username));

        return await _userManager.DoesUsernameExistAsync(username);
    }

    public async Task<UserAppDto?> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(id));

        var user = await _userManager.GetUserByIdAsync(id).ConfigureAwait(false);

        return user?.ToDto();
    }

    public async Task<IList<UserAppDto>> GetUsersAsync()
    {
        var users = await _userManager.GetUsersAsync().ConfigureAwait(false);

        return users.Select(user => user.ToDto()).ToList();
    }

    public async Task<IList<UserAppDto>> GetUsersByRoleAsync(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        await _userManager.EnsureSystemRolesAsync().ConfigureAwait(false);
        var users = await _userManager.GetUsersAsync().ConfigureAwait(false);
        return users
            .Where(user => user.RoleNames.Any(role =>
                string.Equals(SystemUserRoleNames.Normalize(role), SystemUserRoleNames.Normalize(roleName), StringComparison.OrdinalIgnoreCase)))
            .Select(user => user.ToDto())
            .ToList();
    }

    public async Task<UserAppDto?> GetUserByUsernameAndPasswordAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username))
            throw new ArgumentNullException(nameof(username));
        if (string.IsNullOrEmpty(password))
            throw new ArgumentNullException(password);

        var user = await _userManager.FindUserByUserNameAndPasswordAsync(username, password);

        return user?.ToDto();
    }

    public async Task<IList<RoleAppDto>> GetRolesAsync()
    {
        var roles = await _userManager.GetRolesAsync().ConfigureAwait(false);

        return roles.Select(role => role.ToDto()).ToList();
    }

    public Task EnsureSystemRolesAsync()
        => _userManager.EnsureSystemRolesAsync();

    public Task<bool> HasUsersInRoleAsync(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        return _userManager.HasUsersInRoleAsync(roleName);
    }

    public Task<bool> IsUserInRoleAsync(Guid userId, string roleName)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        return _userManager.IsUserInRoleAsync(userId, roleName);
    }

    public async Task<UpdateUserRolesResultAppDto> UpdateUserRolesAsync(UpdateUserRolesAppDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        var (valid, errorMessage) = UpdateUserRolesAppDto.Validate(dto);
        if (!valid)
        {
            return new UpdateUserRolesResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        try
        {
            await _userManager.UpdateUserRolesAsync(new UpdateUserRolesDto(dto.UserId, dto.RoleIds)).ConfigureAwait(false);
            return new UpdateUserRolesResultAppDto
            {
                Success = true
            };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new UpdateUserRolesResultAppDto
            {
                Success = false,
                ErrorMessage = ex.ErrorCode
            };
        }
    }

    public async Task<UserManagementResultAppDto> LockUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return new UserManagementResultAppDto { Success = false, ErrorMessage = "Error.UserRequired" };

        try
        {
            await _userManager.LockUserAsync(userId).ConfigureAwait(false);
            return new UserManagementResultAppDto { Success = true };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new UserManagementResultAppDto { Success = false, ErrorMessage = ex.ErrorCode };
        }
    }

    public async Task<UserManagementResultAppDto> UnlockUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return new UserManagementResultAppDto { Success = false, ErrorMessage = "Error.UserRequired" };

        try
        {
            await _userManager.UnlockUserAsync(userId).ConfigureAwait(false);
            return new UserManagementResultAppDto { Success = true };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new UserManagementResultAppDto { Success = false, ErrorMessage = ex.ErrorCode };
        }
    }

    public async Task<UserManagementResultAppDto> DeleteUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return new UserManagementResultAppDto { Success = false, ErrorMessage = "Error.UserRequired" };

        try
        {
            await _userManager.DeleteUserAsync(userId).ConfigureAwait(false);
            return new UserManagementResultAppDto { Success = true };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new UserManagementResultAppDto { Success = false, ErrorMessage = ex.ErrorCode };
        }
    }

    public async Task<UserManagementResultAppDto> ChangePasswordAsync(Guid userId, string newPassword)
    {
        if (userId == Guid.Empty)
            return new UserManagementResultAppDto { Success = false, ErrorMessage = "Error.UserRequired" };
        if (string.IsNullOrEmpty(newPassword))
            return new UserManagementResultAppDto { Success = false, ErrorMessage = "Error.PasswordRequired" };

        try
        {
            await _userManager.ChangePasswordAsync(userId, newPassword).ConfigureAwait(false);
            return new UserManagementResultAppDto { Success = true };
        }
        catch (NamEcommerceDomainException ex)
        {
            return new UserManagementResultAppDto { Success = false, ErrorMessage = ex.ErrorCode };
        }
    }
}
