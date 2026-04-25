using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Controllers;

public class BaseController : Controller
{
    [FromServices]
    public IStringLocalizer<SharedResource> Localizer { get; set; } = default!;

    public IActionResult RedirectToHome() => RedirectToAction(ControllerConstants.Index, ControllerConstants.Home);

    /// <summary>
    /// Localizes an error code and adds it to ModelState.
    /// </summary>
    protected void AddLocalizedModelError(string? errorCode, params object[] args)
    {
        if (string.IsNullOrEmpty(errorCode)) return;
        var message = args.Length > 0
            ? Localizer[errorCode, args].Value
            : Localizer[errorCode].Value;
        ModelState.AddModelError(string.Empty, message);
    }

    /// <summary>
    /// Localizes an error code for use in TempData or other string contexts.
    /// </summary>
    protected string LocalizeError(string errorCode, params object[] args)
    {
        return args.Length > 0
            ? Localizer[errorCode, args].Value
            : Localizer[errorCode].Value;
    }

    private List<string> GetErrorMessages()
        => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

    public string GetErrorMessage()
        => string.Join(", ", GetErrorMessages());
}
