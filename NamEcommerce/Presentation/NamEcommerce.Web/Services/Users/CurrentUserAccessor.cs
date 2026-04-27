using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Services.Users;

public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly ICurrentUserService _currentUserService;

    public CurrentUserAccessor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserInfoDto?> GetCurrentUserAsync()
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();

        if (currentUser is null)
            return null;

        return new CurrentUserInfoDto(currentUser.Id, currentUser.Username, currentUser.FullName);
    }
}
