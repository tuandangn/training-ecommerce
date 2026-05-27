using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NamEcommerce.Customer.Api.Infrastructure;
using NamEcommerce.Customer.Contracts.Commands;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IMediator mediator,
    CustomerPortalAuthCookieOptions authCookieOptions) : ControllerBase
{
    [HttpPost("otp/request")]
    [EnableRateLimiting("CustomerOtp")]
    public async Task<IActionResult> RequestOtp(RequestOtpRequest request)
    {
        var result = await mediator.Send(new RequestCustomerOtpCommand(request.DeliveryToken, GetIp(), GetUserAgent())).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("otp/verify")]
    [EnableRateLimiting("CustomerOtp")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request)
    {
        var result = await mediator.Send(new VerifyCustomerOtpCommand(request.ChallengeId, request.Otp, GetIp(), GetUserAgent())).ConfigureAwait(false);
        if (!result.Success || result.Session is null || string.IsNullOrWhiteSpace(result.SessionToken))
            return Unauthorized(result);

        AppendSessionCookie(result.SessionToken);
        return Ok(result.Session);
    }

    [HttpPost("password/login")]
    [EnableRateLimiting("CustomerOtp")]
    public async Task<IActionResult> PasswordLogin(PasswordLoginRequest request)
    {
        var result = await mediator.Send(new CustomerPasswordLoginCommand(request.Login, request.Password, GetIp(), GetUserAgent())).ConfigureAwait(false);
        if (!result.Success || result.Session is null || string.IsNullOrWhiteSpace(result.SessionToken))
            return Unauthorized(result);

        AppendSessionCookie(result.SessionToken);
        return Ok(result.Session);
    }

    [HttpPost("password/set")]
    public async Task<IActionResult> SetPassword(SetPasswordRequest request)
    {
        var result = await mediator.Send(new SetCustomerPasswordCommand(request.Password)).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("password/change")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var result = await mediator.Send(new ChangeCustomerPasswordCommand(request.CurrentPassword, request.NewPassword)).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var result = await mediator.Send(new LogoutCustomerCommand()).ConfigureAwait(false);
        Response.Cookies.Delete(CustomerPortalAuthDefaults.SessionCookieName, BuildCookieOptions());
        return Ok(result);
    }

    private void AppendSessionCookie(string token)
        => Response.Cookies.Append(CustomerPortalAuthDefaults.SessionCookieName, token, BuildCookieOptions());

    private CookieOptions BuildCookieOptions()
        => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = authCookieOptions.CrossSite ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/"
        };

    private string? GetIp()
        => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent()
        => Request.Headers.UserAgent.ToString();

    public sealed record RequestOtpRequest(string DeliveryToken);
    public sealed record VerifyOtpRequest(Guid ChallengeId, string Otp);
    public sealed record PasswordLoginRequest(string Login, string Password);
    public sealed record SetPasswordRequest(string Password);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
