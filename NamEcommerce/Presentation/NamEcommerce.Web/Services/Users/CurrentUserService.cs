using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Web.Services.Users;

public sealed class CurrentUserService(ICurrentUserAccessor currentUserAccessor, IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public async ValueTask<CurrentUserInfoAppDto?> GetCurrentUserInfoAsync()
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        if (currentUser is null)
            return null;

        return new CurrentUserInfoAppDto(currentUser.Id, currentUser.Username, currentUser.FullName);
    }

    public ValueTask<bool> IsAuthenticatedAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return ValueTask.FromResult(false);

        return ValueTask.FromResult(httpContext.User?.Identity?.IsAuthenticated ?? false);
    }
}
