using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Web.Services.Users;

public sealed class CurrentUserService(ICurrentUserAccessor currentUserAccessor, IUserAppService userAppService) : ICurrentUserService
{
    public async ValueTask<CurrentUserInfoAppDto?> GetCurrentUserInfoAsync()
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        if (currentUser is null)
            return null;

        return new CurrentUserInfoAppDto(currentUser.Id, currentUser.Username, currentUser.FullName);
    }

    public async Task<bool> IsAdminAsync()
    {
        var currentUser = await GetCurrentUserInfoAsync();
        if (currentUser is null)
            return false;
        return await userAppService.IsUserInRoleAsync(currentUser!.Id, SystemUserRoleNames.Admin);
    }

    public async ValueTask<bool> IsAuthenticatedAsync()
    {
        var currentUser = await GetCurrentUserInfoAsync();
        return currentUser != null;
    }
}
