using Microsoft.AspNetCore.Http;
using NamEcommerce.Web.Contracts.Dtos;
using NamEcommerce.Web.Contracts.Services;
using System.Security.Claims;

namespace NamEcommerce.Web.Framework.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async ValueTask<CurrentUserInfoDto?> GetCurrentUserInfoAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return null;

        var claimsPrincipal = httpContext.User;
        if(claimsPrincipal is null)
            return null;

        var isValidId = Guid.TryParse(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var username = claimsPrincipal.FindFirstValue(ClaimTypes.Email);
        var fullName = claimsPrincipal.FindFirstValue(ClaimTypes.Name);

        if (!isValidId || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName))
            return null;

        return new CurrentUserInfoDto(id, username, fullName);
    }

    public ValueTask<bool> IsAuthenticatedAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return ValueTask.FromResult(false);

        return ValueTask.FromResult(httpContext.User?.Identity?.IsAuthenticated ?? false);
    }
}
