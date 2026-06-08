using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Services.Users;
using System.Security.Claims;

namespace NamEcommerce.Web.Services.Users;

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public async Task<CurrentUserInfoDto?> GetCurrentUserAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        var claimsPrincipal = httpContext.User;
        if (claimsPrincipal is null)
            return null;

        var isValidId = Guid.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var username = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
        var fullName = claimsPrincipal.FindFirstValue(ClaimTypes.Name);

        if (!isValidId || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName))
            return null;

        return new CurrentUserInfoDto(id, username, fullName);
    }
}
