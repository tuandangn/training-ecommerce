using MediatR;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Models.Users;
using NamEcommerce.Web.Contracts.Queries.Models.Users;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Users;

public sealed class GetAdminUserListHandler(IUserAppService userAppService)
    : IRequestHandler<GetAdminUserListQuery, AdminUserListModel>
{
    public async Task<AdminUserListModel> Handle(GetAdminUserListQuery request, CancellationToken cancellationToken)
    {
        var users = await userAppService.GetUsersAsync().ConfigureAwait(false);

        return new AdminUserListModel
        {
            Users = users.Select(user => new AdminUserListItemModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                IsLocked = user.IsLocked,
                RoleNames = user.RoleNames
            }).ToList()
        };
    }
}

public sealed class GetChangeUserPasswordModelHandler(IUserAppService userAppService)
    : IRequestHandler<GetChangeUserPasswordModelQuery, ChangeUserPasswordModel?>
{
    public async Task<ChangeUserPasswordModel?> Handle(GetChangeUserPasswordModelQuery request, CancellationToken cancellationToken)
    {
        var user = await userAppService.GetUserByIdAsync(request.UserId).ConfigureAwait(false);
        if (user is null)
            return null;

        return new ChangeUserPasswordModel
        {
            UserId = user.Id,
            Username = user.Username
        };
    }
}
