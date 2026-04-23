using Microsoft.AspNetCore.Http;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Web.Contracts.Models.Users;
using NamEcommerce.Web.Contracts.Services;
using System.Security.Claims;

namespace NamEcommerce.Web.Framework.Services;

public sealed class CurrentUserService : ICurrentUserService, ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<CurrentUserInfoDto?> GetCurrentUserAsync()
    {
        var currentUser = await GetCurrentUserInfoAsync();

        if (currentUser is null)
            return null;

        return new CurrentUserInfoDto(currentUser.Id, currentUser.Username, currentUser.FullName);
    }

    public async ValueTask<CurrentUserInfoModel?> GetCurrentUserInfoAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
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

        return new CurrentUserInfoModel(id, username, fullName);
    }

    public ValueTask<bool> IsAuthenticatedAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return ValueTask.FromResult(false);

        return ValueTask.FromResult(httpContext.User?.Identity?.IsAuthenticated ?? false);
    }
}
