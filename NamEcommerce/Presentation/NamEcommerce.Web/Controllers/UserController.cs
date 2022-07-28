using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Models.Users;

namespace NamEcommerce.Web.Controllers;

public sealed class UserController : Controller
{
    public IActionResult Login() => View();

    public IActionResult Register()
        => View(new RegisterModel());

    [HttpPost]
    public IActionResult Register(RegisterModel model)
    {
        throw new NotImplementedException();
    }

    public IActionResult Logout()
    {
        HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Index), "Home");
    }
}
