using NamEcommerce.Application.Contracts.Security;
using NamEcommerce.Application.Contracts.Users;

namespace NamEcommerce.Web.Services.Permissions;

public sealed class AuthorizationAppService(IPermissionCacheService permissionCacheService, IUserAppService userAppService) : IAuthorizationAppService
{
    public async Task<bool> Authorize(Guid userId, string permissionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(permissionName, nameof(permissionName));

        var roles = await userAppService.GetRoleNamesByUserIdAsync(userId);

        if (!roles.Any())
            return false;

        foreach (var role in roles)
        {
            var permissions = await permissionCacheService.GetPermissionsForRoleAsync(role);
            if (permissions.Contains(permissionName, StringComparer.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
