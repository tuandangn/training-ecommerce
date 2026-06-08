using Microsoft.AspNetCore.Authorization;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Web.Services.Permissions;
using System.Security.Claims;

namespace NamEcommerce.Web.Authorization;

public sealed class PermissionAuthorizationHandler(IPermissionCacheService permissionCache)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return;

        if (context.User.IsInRole(SystemUserRoleNames.Admin))
        {
            context.Succeed(requirement);
            return;
        }

        var userRoles = context.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        foreach (var role in userRoles)
        {
            var permissions = await permissionCache.GetPermissionsForRoleAsync(role).ConfigureAwait(false);
            var normalizedPermission = requirement.Permission.ToUpperInvariant();
            if (permissions.Contains(normalizedPermission))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
