using Microsoft.AspNetCore.Authorization;
using NamEcommerce.Application.Contracts.Security;
using NamEcommerce.Application.Contracts.Users;

namespace NamEcommerce.Web.Authorization;

public sealed class PermissionAuthorizationHandler(ICurrentUserService currentUserService, IAuthorizationAppService authorizationAppService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (await currentUserService.IsAdminAsync())
        {
            context.Succeed(requirement);
            return;
        }

        var currentUser = await currentUserService.GetCurrentUserInfoAsync();
        if (currentUser is null)
            return;

        var authorized = await authorizationAppService.Authorize(currentUser.Id, requirement.Permission);
        if (authorized)
            context.Succeed(requirement);
    }
}
