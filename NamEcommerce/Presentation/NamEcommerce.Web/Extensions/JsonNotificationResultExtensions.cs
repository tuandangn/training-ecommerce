using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Extensions;

/// <summary>
/// Convenience extensions to produce <see cref="JsonNotificationResult"/>
/// payloads consistently from any controller action.
/// </summary>
public static class JsonNotificationResultExtensions
{
    /// <summary>
    /// Returns a JSON success envelope. Optionally includes a notification message.
    /// </summary>
    public static JsonResult JsonOk(
        this Controller controller,
        object? data = null,
        string? message = null,
        string? title = null,
        int durationMs = 4000)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return controller.Json(new JsonNotificationResult
        {
            Success = true,
            Data = data,
            Notification = string.IsNullOrEmpty(message)
                ? null
                : new NotificationModel
                {
                    Type = NotificationType.Success,
                    Message = message,
                    Title = title,
                    DurationMs = durationMs
                }
        });
    }

    /// <summary>
    /// Returns a JSON error envelope with a notification message.
    /// </summary>
    public static JsonResult JsonError(
        this Controller controller,
        string message,
        string? title = null,
        int durationMs = 5000,
        object? data = null)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return controller.Json(new JsonNotificationResult
        {
            Success = false,
            Data = data,
            Notification = new NotificationModel
            {
                Type = NotificationType.Error,
                Message = message ?? string.Empty,
                Title = title,
                DurationMs = durationMs
            }
        });
    }

    /// <summary>
    /// Returns a JSON envelope with an arbitrary notification (warning/info/etc.).
    /// </summary>
    public static JsonResult JsonNotify(
        this Controller controller,
        bool success,
        NotificationType type,
        string message,
        string? title = null,
        int durationMs = 4000,
        object? data = null)
    {
        ArgumentNullException.ThrowIfNull(controller);

        return controller.Json(new JsonNotificationResult
        {
            Success = success,
            Data = data,
            Notification = new NotificationModel
            {
                Type = type,
                Message = message ?? string.Empty,
                Title = title,
                DurationMs = durationMs
            }
        });
    }
}
