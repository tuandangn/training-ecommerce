using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models;
using NamEcommerce.Web.Contracts.Models.Users;
using System.Security.Claims;

namespace NamEcommerce.Web.Framework.Commands.Handlers;

public sealed class CookieAuthenticateUserHandler : IRequestHandler<AuthenticateUserCommand, AuthenticateUserResult>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserAppService _userAppService;

    public CookieAuthenticateUserHandler(IHttpContextAccessor httpContextAccessor, IUserAppService userAppService)
    {
        _httpContextAccessor = httpContextAccessor;
        _userAppService = userAppService;
    }

    public async Task<AuthenticateUserResult> Handle(AuthenticateUserCommand request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return new AuthenticateUserResult
            {
                Success = false,
                ErrorMessage = "Cannot sign user in here"
            };
        }
        if (string.IsNullOrEmpty(request.Username))
            throw new ArgumentException("Username is required", nameof(request.Username));

        if (string.IsNullOrEmpty(request.Password))
            throw new ArgumentException("Password is required", nameof(request.Password));

        var userDto = await _userAppService.GetUserByUsernameAndPasswordAsync(request.Username, request.Password);
        if (userDto is null)
        {
            return new AuthenticateUserResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }

        await httpContext.SignOutAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userDto.FullName),
            new Claim(ClaimTypes.Email, userDto.Username),
            new Claim(ClaimTypes.NameIdentifier, userDto.Id.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            
        };
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return new AuthenticateUserResult
        {
            Success = true
        };
    }
}
