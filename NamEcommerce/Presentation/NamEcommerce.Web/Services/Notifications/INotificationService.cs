using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Services.Notifications;

/// <summary>
/// Service for managing user-facing notifications across requests.
/// Notifications are persisted via TempData and consumed once on the next render.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Adds a success notification.
    /// </summary>
    void Success(string message, string? title = null, int? durationMs = null);

    /// <summary>
    /// Adds an error notification.
    /// </summary>
    void Error(string message, string? title = null, int? durationMs = null);

    /// <summary>
    /// Adds a warning notification.
    /// </summary>
    void Warning(string message, string? title = null, int? durationMs = null);

    /// <summary>
    /// Adds an informational notification.
    /// </summary>
    void Info(string message, string? title = null, int? durationMs = null);

    /// <summary>
    /// Adds a pre-built notification.
    /// </summary>
    void Add(NotificationModel notification);

    /// <summary>
    /// Returns all pending notifications and clears them from storage.
    /// Call from layout/partial when rendering notifications to the client.
    /// </summary>
    IReadOnlyList<NotificationModel> ConsumeAll();
}
