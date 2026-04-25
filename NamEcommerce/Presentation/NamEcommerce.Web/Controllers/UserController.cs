using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Users;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Models.Users;

namespace NamEcommerce.Web.Controllers;

public sealed class UserController : BaseController
{
    private readonly IMediator _mediator;
    private readonly AppConfig _appConfig;
    private readonly IUserAppService userAppService;

    public UserController(IMediator mediator, AppConfig appConfig, IUserAppService userAppService)
    {
        _mediator = mediator;
        _appConfig = appConfig;
        this.userAppService = userAppService;
    }

    public IActionResult Index() => RedirectToAction(nameof(Login));

    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var authenticateUserResult = await _mediator.Send(new AuthenticateUserCommand(model.Username!, model.Password!));
        if (!authenticateUserResult.Success)
        {
            AddLocalizedModelError(authenticateUserResult.ErrorMessage);
            return View(model);
        }

        if (Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToHome();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToHome();
    }

    public IActionResult Register()
    {
        if (!_appConfig.AllowRegisterUser)
        {
            NotifyError("Error.UserRegistrationDisabled");
            return RedirectToHome();
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (!_appConfig.AllowRegisterUser)
            return RedirectToHome();

        if (!ModelState.IsValid)
            return View(model);

        var registerUserResult = await _mediator.Send(new RegisterUserCommand(model.Username!, model.Password!, model.Fullname!)
        {
            PhoneNumber = model.PhoneNumber!,
            Address = model.Address
        });
        if (registerUserResult.Success)
            return RedirectToHome();

        if (registerUserResult.CreatedId is not null)
        {
            NotifyError(registerUserResult.ErrorMessage!);
            return RedirectToAction(nameof(Login));
        }

        AddLocalizedModelError(registerUserResult.ErrorMessage);
        return View(model);
    }
}
