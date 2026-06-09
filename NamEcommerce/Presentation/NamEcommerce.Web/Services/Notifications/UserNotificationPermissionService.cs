using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Services.Permissions;

namespace NamEcommerce.Web.Services.Notifications;

public sealed class UserNotificationPermissionService(
    IHttpContextAccessor httpContextAccessor,
    IPermissionCacheService permissionCache) : IUserNotificationPermissionService
{
    public Task<UserNotificationPermissionSnapshot> GetCurrentAsync(CancellationToken cancellationToken = default)
        => GetForUserAsync(httpContextAccessor.HttpContext?.User, cancellationToken);

    public async Task<UserNotificationPermissionSnapshot> GetForUserAsync(
        ClaimsPrincipal? user,
        CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
            return new UserNotificationPermissionSnapshot(null, []);

        var userId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : (Guid?)null;
        var roles = user.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roles.Any(IsAdminRole))
            return new UserNotificationPermissionSnapshot(userId, NormalizePermissions(SystemPermissions.GetAll()));

        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            var rolePermissions = await permissionCache.GetPermissionsForRoleAsync(role, cancellationToken)
                .ConfigureAwait(false);

            foreach (var permission in rolePermissions)
                if (!string.IsNullOrWhiteSpace(permission))
                    permissions.Add(permission.ToUpperInvariant());
        }

        return new UserNotificationPermissionSnapshot(userId, permissions.ToList());
    }

    private static bool IsAdminRole(string role)
        => string.Equals(
            SystemUserRoleNames.Normalize(role),
            SystemUserRoleNames.Normalize(SystemUserRoleNames.Admin),
            StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyCollection<string> NormalizePermissions(IEnumerable<string> permissions)
        => permissions
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Select(permission => permission.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
