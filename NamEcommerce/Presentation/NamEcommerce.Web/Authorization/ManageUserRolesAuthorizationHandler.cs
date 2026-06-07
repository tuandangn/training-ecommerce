using Microsoft.AspNetCore.Authorization;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Dtos.Users;
using System.Security.Claims;

namespace NamEcommerce.Web.Authorization;

public sealed class ManageUserRolesRequirement : IAuthorizationRequirement;

public sealed class ManageUserRolesAuthorizationHandler(
    IUserAppService userAppService) : AuthorizationHandler<ManageUserRolesRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ManageUserRolesRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
            return;

        var hasAdmin = await userAppService.HasUsersInRoleAsync(SystemUserRoleNames.Admin).ConfigureAwait(false);
        if (!hasAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId))
            return;

        if (await userAppService.IsUserInRoleAsync(currentUserId, SystemUserRoleNames.Admin).ConfigureAwait(false))
            context.Succeed(requirement);
    }
}
