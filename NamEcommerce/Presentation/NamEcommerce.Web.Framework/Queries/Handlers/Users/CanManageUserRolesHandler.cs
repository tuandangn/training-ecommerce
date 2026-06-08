using MediatR;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Queries.Models.Users;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Users;

public sealed class CanManageUserRolesHandler(
    ICurrentUserService currentUserService,
    IUserAppService userAppService) : IRequestHandler<CanManageUserRolesQuery, bool>
{
    private const string AdminRoleName = "Admin";

    public async Task<bool> Handle(CanManageUserRolesQuery request, CancellationToken cancellationToken)
    {
        if (!await currentUserService.IsAuthenticatedAsync().ConfigureAwait(false))
            return false;

        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        if (currentUser is null)
            return false;

        var hasAdmin = await userAppService.HasUsersInRoleAsync(AdminRoleName).ConfigureAwait(false);
        if (!hasAdmin)
            return true;

        return await userAppService.IsUserInRoleAsync(currentUser.Id, AdminRoleName).ConfigureAwait(false);
    }
}
