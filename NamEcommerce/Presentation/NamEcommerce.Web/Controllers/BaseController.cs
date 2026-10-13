using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Resources;
using NamEcommerce.Web.Services.Notifications;

namespace NamEcommerce.Web.Controllers;

public class BaseController : Controller
{
    [FromServices]
    public IStringLocalizer<SharedResource> Localizer { get; set; } = default!;

    [FromServices]
    public INotificationService NotificationService { get; set; } = default!;

    [FromServices]
    public IServiceProvider ServiceProvider { get; set; } = default!;

    protected IActionResult RedirectToHome() => RedirectToAction(ControllerConstants.Index, ControllerConstants.Home);

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

    /// <summary>
    /// Adds a localized success notification (auto-localizes via <see cref="Localizer"/>).
    /// </summary>
    protected void NotifySuccess(string localizationKey, params object[] args)
    {
        if (string.IsNullOrEmpty(localizationKey)) return;
        NotificationService.Success(LocalizeError(localizationKey, args));
    }

    /// <summary>
    /// Adds a localized error notification (auto-localizes via <see cref="Localizer"/>).
    /// </summary>
    protected void NotifyError(string localizationKey, params object[] args)
    {
        if (string.IsNullOrEmpty(localizationKey)) return;
        NotificationService.Error(LocalizeError(localizationKey, args));
    }

    /// <summary>
    /// Adds a localized warning notification (auto-localizes via <see cref="Localizer"/>).
    /// </summary>
    protected void NotifyWarning(string localizationKey, params object[] args)
    {
        if (string.IsNullOrEmpty(localizationKey)) return;
        NotificationService.Warning(LocalizeError(localizationKey, args));
    }

    /// <summary>
    /// Adds a localized informational notification (auto-localizes via <see cref="Localizer"/>).
    /// </summary>
    protected void NotifyInfo(string localizationKey, params object[] args)
    {
        if (string.IsNullOrEmpty(localizationKey)) return;
        NotificationService.Info(LocalizeError(localizationKey, args));
    }

    private List<string> GetErrorMessages()
        => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

    protected string GetErrorMessage()
        => string.Join(", ", GetErrorMessages());

    protected virtual async Task<string> RenderPartialViewToStringAsync(string viewName, object model)
    {
        //get Razor view engine
        var razorViewEngine = ServiceProvider.GetRequiredService<IRazorViewEngine>();

        //create action context
        var actionContext = new ActionContext(HttpContext, RouteData, ControllerContext.ActionDescriptor, ModelState);

        //set view name as action name in case if not passed
        if (string.IsNullOrEmpty(viewName))
            viewName = ControllerContext.ActionDescriptor.ActionName;

        //set model
        ViewData.Model = model;

        //try to get a view by the name
        var viewResult = razorViewEngine.FindView(actionContext, viewName, false);

        if (viewResult.View == null)
        {
            //or try to get a view by the path
            viewResult = razorViewEngine.GetView(null, viewName, false);
            if (viewResult.View == null)
                throw new ArgumentNullException($"{viewName} view was not found");
        }

        await using var stringWriter = new StringWriter();
        var viewContext = new ViewContext(actionContext, viewResult.View, ViewData, TempData, stringWriter, new HtmlHelperOptions());
        await viewResult.View.RenderAsync(viewContext);

        return stringWriter.GetStringBuilder().ToString();
    }
}
