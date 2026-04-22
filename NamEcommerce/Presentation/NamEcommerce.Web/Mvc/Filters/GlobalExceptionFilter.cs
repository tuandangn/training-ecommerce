using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Web.Resources;
using NamEcommerce.Web.Constants;

namespace NamEcommerce.Web.Mvc.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ITempDataDictionaryFactory _tempDataDictionaryFactory;

    public GlobalExceptionFilter(
        IStringLocalizer<SharedResource> localizer,
        ITempDataDictionaryFactory tempDataDictionaryFactory)
    {
        _localizer = localizer;
        _tempDataDictionaryFactory = tempDataDictionaryFactory;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is NamEcommerceDomainException domainEx)
        {
            var localizedParams = domainEx.Parameters?.Select(p => 
                p is string s && (s.StartsWith("Label.") || s.StartsWith("Error.") || s.StartsWith("Msg."))
                ? _localizer[s].Value
                : p).ToArray();

            var localizedMessage = localizedParams != null && localizedParams.Length > 0
                ? _localizer[domainEx.ErrorCode, localizedParams].Value
                : _localizer[domainEx.ErrorCode].Value;

            // If it's an AJAX request expecting JSON
            var request = context.HttpContext.Request;
            bool isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
                          request.Headers["Accept"].ToString().Contains("application/json");

            if (isAjax)
            {
                context.Result = new JsonResult(new { success = false, message = localizedMessage });
            }
            else
            {
                // Set TempData
                var tempData = _tempDataDictionaryFactory.GetTempData(context.HttpContext);
                tempData[ViewConstants.GlobalErrorMessage] = localizedMessage;

                // Optionally, we could redirect back to the referer
                var referer = request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    context.Result = new RedirectResult(referer);
                }
                else
                {
                    // Fallback to Home
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                }
            }

            context.ExceptionHandled = true;
        }
    }
}
