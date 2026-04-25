using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Resources;
using NamEcommerce.Web.Services.Notifications;

namespace NamEcommerce.Web.Mvc.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly INotificationService _notificationService;

    public GlobalExceptionFilter(
        IStringLocalizer<SharedResource> localizer,
        INotificationService notificationService)
    {
        _localizer = localizer;
        _notificationService = notificationService;
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
                // Trả về theo chuẩn JsonNotificationResult — ajax-helper.js sẽ tự render notification
                context.Result = new JsonResult(new JsonNotificationResult
                {
                    Success = false,
                    Notification = new NotificationModel
                    {
                        Type = NotificationType.Error,
                        Message = localizedMessage,
                        DurationMs = 5000
                    }
                });
            }
            else
            {
                _notificationService.Error(localizedMessage);

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
