using MediatR;
using NamEcommerce.Web.Contracts.Models.Users;
using NamEcommerce.Web.Contracts.Queries.Models.Users;

namespace NamEcommerce.Web.Services.Users;

public sealed class UserManagementModelFactory(IMediator mediator) : IUserManagementModelFactory
{
    public Task<UserRoleListModel> PrepareUserRoleListModel()
        => mediator.Send(new GetUserRoleListQuery());

    public async Task<UserRoleAssignmentModel?> PrepareUserRoleAssignmentModel(Guid userId, UserRoleAssignmentModel? oldModel = null)
    {
        var model = await mediator.Send(new GetUserRoleAssignmentQuery(userId)).ConfigureAwait(false);
        if (model is null || oldModel is null)
            return model;

        var selectedRoleIds = oldModel.RoleIds.ToHashSet();

        return model with
        {
            RoleIds = oldModel.RoleIds,
            AvailableRoles = model.AvailableRoles.Select(role => role with
            {
                Selected = selectedRoleIds.Contains(role.Id)
            }).ToList()
        };
    }

    public Task<RolePermissionsPageModel?> PrepareRolePermissionsPageModel(Guid roleId)
        => mediator.Send(new GetRolePermissionsPageQuery(roleId));
}
