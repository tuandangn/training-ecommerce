using MediatR;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Models.Users;
using NamEcommerce.Web.Contracts.Queries.Models.Users;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Users;

public sealed class GetUserRoleListHandler(IUserAppService userAppService)
    : IRequestHandler<GetUserRoleListQuery, UserRoleListModel>
{
    public async Task<UserRoleListModel> Handle(GetUserRoleListQuery request, CancellationToken cancellationToken)
    {
        await userAppService.EnsureSystemRolesAsync().ConfigureAwait(false);
        var users = await userAppService.GetUsersAsync().ConfigureAwait(false);

        return new UserRoleListModel
        {
            Users = users.Select(user => new UserRoleListItemModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                RoleNames = user.RoleNames
            }).ToList()
        };
    }
}

public sealed class GetUserRoleAssignmentHandler(IUserAppService userAppService)
    : IRequestHandler<GetUserRoleAssignmentQuery, UserRoleAssignmentModel?>
{
    public async Task<UserRoleAssignmentModel?> Handle(GetUserRoleAssignmentQuery request, CancellationToken cancellationToken)
    {
        await userAppService.EnsureSystemRolesAsync().ConfigureAwait(false);

        var user = await userAppService.GetUserByIdAsync(request.UserId).ConfigureAwait(false);
        if (user is null)
            return null;

        var selectedRoleNames = user.RoleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var roles = await userAppService.GetRolesAsync().ConfigureAwait(false);

        return new UserRoleAssignmentModel
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            RoleIds = roles
                .Where(role => selectedRoleNames.Contains(role.Name))
                .Select(role => role.Id)
                .ToList(),
            AvailableRoles = roles.Select(role => new RoleSelectionModel
            {
                Id = role.Id,
                Name = role.Name,
                Selected = selectedRoleNames.Contains(role.Name)
            }).ToList()
        };
    }
}
