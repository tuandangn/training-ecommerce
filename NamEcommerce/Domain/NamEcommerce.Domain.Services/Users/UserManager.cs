using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Exceptions.Users;
using NamEcommerce.Domain.Shared.Services.Security;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Domain.Services.Users;

public sealed class UserManager : IUserManager
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IRepository<UserRole> _userRoleRepository;
    private readonly IEntityDataReader<User> _userEntityDataReader;
    private readonly IEntityDataReader<Role> _roleEntityDataReader;
    private readonly IEntityDataReader<UserRole> _userRoleEntityDataReader;
    private readonly ISecurityService _securityService;

    public UserManager(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IRepository<UserRole> userRoleRepository,
        IEntityDataReader<User> userEntityDataReader,
        IEntityDataReader<Role> roleEntityDataReader,
        IEntityDataReader<UserRole> userRoleEntityDataReader,
        ISecurityService securityService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _userEntityDataReader = userEntityDataReader;
        _roleEntityDataReader = roleEntityDataReader;
        _userRoleEntityDataReader = userRoleEntityDataReader;
        _securityService = securityService;
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
        user.MarkCreated();
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

        var user = await _userEntityDataReader.DataSource.FirstOrDefaultAsync(user => user.Username == username).ConfigureAwait(false);
        if (user is null) 
            return false;
        return !comparesWithCurrentId.HasValue || comparesWithCurrentId != user.Id;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(id));

        var user = await _userRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (user is null)
            return null;

        var roleNames = await GetRoleNamesByUserIdAsync(user.Id).ConfigureAwait(false);
        return user.ToDto(roleNames);
    }

    public async Task<IList<UserDto>> GetUsersAsync()
    {
        var users = await _userEntityDataReader.DataSource
            .OrderBy(user => user.FullName)
            .ThenBy(user => user.Username)
            .ToListAsync().ConfigureAwait(false);
        var userIds = users.Select(user => user.Id).ToHashSet();

        var userRoles = await _userRoleEntityDataReader.DataSource
            .Where(userRole => userIds.Contains(userRole.UserId))
            .ToListAsync().ConfigureAwait(false);

        var roleIds = userRoles.Select(userRole => userRole.RoleId).Distinct().ToHashSet();
        var roleNames = await _roleEntityDataReader.DataSource
            .Where(role => roleIds.Contains(role.Id))
            .ToDictionaryAsync(role => role.Id, role => role.Name).ConfigureAwait(false);

        var roleNamesByUserId = userRoles
            .Where(userRole => roleNames.ContainsKey(userRole.RoleId))
            .GroupBy(userRole => userRole.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IList<string>)group
                    .Select(userRole => roleNames[userRole.RoleId])
                    .OrderBy(name => name)
                    .ToList());

        var result = users
            .Select(user => user.ToDto(roleNamesByUserId.GetValueOrDefault(user.Id) ?? []))
            .ToList();

        return result;
    }

    public async Task<UserDto?> FindUserByUserNameAndPasswordAsync(string username, string password)
    {
        if (username is null)
            throw new ArgumentNullException(nameof(username));

        if (password is null)
            throw new ArgumentNullException(nameof(password));

        var query = from user in _userEntityDataReader.DataSource
                    where user.Username == username && !user.IsLocked
                    select user;
        var users = await query.ToListAsync().ConfigureAwait(false);
        foreach (var user in users)
        {
            var valid = await _securityService.VerifyPasswordAsync(password, user.PasswordHash, user.PasswordSalt).ConfigureAwait(false);
            if (valid)
            {
                var roleNames = await GetRoleNamesByUserIdAsync(user.Id).ConfigureAwait(false);
                return user.ToDto(roleNames);
            }
        }

        return null;
    }

    public async Task<IList<RoleDto>> GetRolesAsync()
    {
        await EnsureSystemRolesAsync().ConfigureAwait(false);

        var roles = await _roleEntityDataReader.DataSource
            .OrderBy(role => role.Name)
            .Select(role => role.ToDto())
            .ToListAsync().ConfigureAwait(false);

        return roles;
    }

    public async Task<IList<string>> GetRoleNamesByUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(userId));

        var roleIds = await _userRoleEntityDataReader.DataSource
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.RoleId)
            .ToListAsync().ConfigureAwait(false);
        var roleNames = await _roleEntityDataReader.DataSource
            .Where(role => roleIds.Contains(role.Id))
            .OrderBy(role => role.Name)
            .Select(role => role.Name)
            .ToListAsync().ConfigureAwait(false);

        return roleNames;
    }

    public async Task<bool> HasUsersInRoleAsync(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        await EnsureSystemRolesAsync().ConfigureAwait(false);

        var roleIds = await GetRoleIdsByNameAsync(roleName).ConfigureAwait(false);
        if (roleIds.Count == 0)
            return false;

        return await _userRoleEntityDataReader.DataSource.AnyAsync(userRole => roleIds.Contains(userRole.RoleId))
            .ConfigureAwait(false);
    }

    public async Task<bool> IsUserInRoleAsync(Guid userId, string roleName)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        await EnsureSystemRolesAsync().ConfigureAwait(false);

        var roleIds = await GetRoleIdsByNameAsync(roleName).ConfigureAwait(false);
        if (roleIds.Count == 0)
            return false;

        return await _userRoleEntityDataReader.DataSource.AnyAsync(userRole => userRole.UserId == userId && roleIds.Contains(userRole.RoleId)).ConfigureAwait(false);
    }

    public async Task EnsureSystemRolesAsync()
    {
        var existingNormalizedNames = await _roleEntityDataReader.DataSource
            .Select(role => string.IsNullOrWhiteSpace(role.NormalizedName)
                ? SystemUserRoleNames.Normalize(role.Name)
                : role.NormalizedName)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase).ConfigureAwait(false);

        foreach (var roleName in SystemUserRoleNames.All)
        {
            var normalizedName = SystemUserRoleNames.Normalize(roleName);
            if (existingNormalizedNames.Contains(normalizedName))
                continue;

            await _roleRepository.InsertAsync(new Role(Guid.NewGuid(), roleName)
            {
                NormalizedName = normalizedName
            }).ConfigureAwait(false);
            existingNormalizedNames.Add(normalizedName);
        }
    }

    public async Task UpdateUserRolesAsync(UpdateUserRolesDto dto)
    {
        if (dto is null)
            throw new ArgumentNullException(nameof(dto));

        dto.Verify();

        var user = await _userRepository.GetByIdAsync(dto.UserId).ConfigureAwait(false);
        if (user is null)
            throw new UserIsNotFoundException(dto.UserId);

        await EnsureSystemRolesAsync().ConfigureAwait(false);

        var requestedRoleIds = dto.RoleIds.Distinct().ToHashSet();
        var validRoleIds = await _roleEntityDataReader.DataSource
            .Where(role => requestedRoleIds.Contains(role.Id))
            .Select(role => role.Id)
            .ToHashSetAsync().ConfigureAwait(false);
        if (validRoleIds.Count != requestedRoleIds.Count)
            throw new UserDataIsInvalidException("Error.RoleIsNotFound");

        var currentUserRoles = await _userRoleEntityDataReader.DataSource
            .Where(userRole => userRole.UserId == dto.UserId)
            .ToListAsync().ConfigureAwait(false);
        var currentRoleIds = currentUserRoles.Select(userRole => userRole.RoleId).ToHashSet();

        foreach (var currentUserRole in currentUserRoles.Where(userRole => !requestedRoleIds.Contains(userRole.RoleId)))
            await _userRoleRepository.DeleteAsync(currentUserRole).ConfigureAwait(false);

        foreach (var roleId in requestedRoleIds.Where(roleId => !currentRoleIds.Contains(roleId)))
            await _userRoleRepository.InsertAsync(new UserRole(dto.UserId, roleId)).ConfigureAwait(false);
    }

    public async Task LockUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(userId));

        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false)
            ?? throw new UserIsNotFoundException(userId);

        user.Lock();
        await _userRepository.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task UnlockUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(userId));

        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false)
            ?? throw new UserIsNotFoundException(userId);

        user.Unlock();
        await _userRepository.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task DeleteUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(userId));

        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false)
            ?? throw new UserIsNotFoundException(userId);

        user.MarkDeleted();
        await _userRepository.DeleteAsync(user).ConfigureAwait(false);
    }

    public async Task ChangePasswordAsync(Guid userId, string newPassword)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id is required", nameof(userId));
        ArgumentException.ThrowIfNullOrEmpty(newPassword);

        var user = await _userRepository.GetByIdAsync(userId).ConfigureAwait(false)
            ?? throw new UserIsNotFoundException(userId);

        await user.SetPasswordAsync(newPassword, _securityService).ConfigureAwait(false);
        user.MarkPasswordChanged();
        await _userRepository.UpdateAsync(user).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<Guid>> GetRoleIdsByNameAsync(string roleName)
    {
        var normalizedName = SystemUserRoleNames.Normalize(roleName);

        return (await _roleEntityDataReader.DataSource.ToListAsync().ConfigureAwait(false))
            .Where(role => string.Equals(
                string.IsNullOrWhiteSpace(role.NormalizedName)
                    ? SystemUserRoleNames.Normalize(role.Name)
                    : role.NormalizedName,
                normalizedName,
                StringComparison.OrdinalIgnoreCase))
            .Select(role => role.Id)
            .ToList();
    }
}
