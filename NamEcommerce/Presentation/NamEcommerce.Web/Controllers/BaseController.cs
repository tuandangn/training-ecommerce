using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;

namespace NamEcommerce.Web.Controllers;

public class BaseController : Controller
{
    public IActionResult RedirectToHome() => RedirectToAction(MvcConstants.Index, MvcConstants.Home);
}
