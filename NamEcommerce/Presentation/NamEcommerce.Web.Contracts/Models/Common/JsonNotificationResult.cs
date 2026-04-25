namespace NamEcommerce.Web.Contracts.Models.Common;

/// <summary>
/// Standard JSON envelope returned from controller AJAX endpoints.
/// The client-side ajax helper inspects <see cref="Notification"/> and
/// renders it via NotificationCenter automatically.
/// </summary>
[Serializable]
public sealed record JsonNotificationResult
{
    public required bool Success { get; init; }
    public NotificationModel? Notification { get; init; }
    public object? Data { get; init; }
}
