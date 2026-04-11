using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;

namespace NamEcommerce.Web.Controllers;

public class BaseController : Controller
{
    public IActionResult RedirectToHome() => RedirectToAction(MvcConstants.Index, MvcConstants.Home);

    private List<string> GetErrorMessages() 
        => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    public string GetErrorMessage() 
        => string.Join(", ", GetErrorMessages());
}
