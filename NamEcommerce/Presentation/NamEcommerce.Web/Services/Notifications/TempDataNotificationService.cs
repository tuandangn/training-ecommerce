using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Services.Notifications;

/// <summary>
/// TempData-backed implementation of <see cref="INotificationService"/>.
/// All notifications are serialized into a single TempData key
/// (<see cref="TempDataKey"/>) so the consumer doesn't need to know
/// per-module key conventions.
/// </summary>
public sealed class TempDataNotificationService : INotificationService
{
    public const string TempDataKey = "Messages.Notifications";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TempDataNotificationService(
        ITempDataDictionaryFactory tempDataFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _tempDataFactory = tempDataFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public void Success(string message, string? title = null, int? durationMs = null)
        => Add(BuildNotification(NotificationType.Success, message, title, durationMs));

    public void Error(string message, string? title = null, int? durationMs = null)
        => Add(BuildNotification(NotificationType.Error, message, title, durationMs));

    public void Warning(string message, string? title = null, int? durationMs = null)
        => Add(BuildNotification(NotificationType.Warning, message, title, durationMs));

    public void Info(string message, string? title = null, int? durationMs = null)
        => Add(BuildNotification(NotificationType.Info, message, title, durationMs));

    public void Add(NotificationModel notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var tempData = GetTempData();
        if (tempData is null) return;

        var pending = ReadPending(tempData);
        pending.Add(notification);

        tempData[TempDataKey] = JsonSerializer.Serialize(pending, SerializerOptions);
    }

    public IReadOnlyList<NotificationModel> ConsumeAll()
    {
        var tempData = GetTempData();
        if (tempData is null) return Array.Empty<NotificationModel>();

        if (!tempData.TryGetValue(TempDataKey, out var raw) || raw is not string json || string.IsNullOrEmpty(json))
            return Array.Empty<NotificationModel>();

        // Remove so notifications are shown only once.
        tempData.Remove(TempDataKey);

        try
        {
            var items = JsonSerializer.Deserialize<List<NotificationModel>>(json, SerializerOptions);
            return items ?? new List<NotificationModel>();
        }
        catch (JsonException)
        {
            return Array.Empty<NotificationModel>();
        }
    }

    private static NotificationModel BuildNotification(
        NotificationType type,
        string message,
        string? title,
        int? durationMs)
        => new()
        {
            Type = type,
            Message = message ?? string.Empty,
            Title = title,
            DurationMs = durationMs ?? 4000
        };

    private List<NotificationModel> ReadPending(ITempDataDictionary tempData)
    {
        if (!tempData.TryGetValue(TempDataKey, out var raw) || raw is not string json || string.IsNullOrEmpty(json))
            return new List<NotificationModel>();

        try
        {
            var items = JsonSerializer.Deserialize<List<NotificationModel>>(json, SerializerOptions);
            return items ?? new List<NotificationModel>();
        }
        catch (JsonException)
        {
            return new List<NotificationModel>();
        }
    }

    private ITempDataDictionary? GetTempData()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext is null ? null : _tempDataFactory.GetTempData(httpContext);
    }
}
