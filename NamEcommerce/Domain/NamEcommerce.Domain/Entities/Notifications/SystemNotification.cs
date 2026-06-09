using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Notifications;

namespace NamEcommerce.Domain.Entities.Notifications;

[Serializable]
public sealed record SystemNotification : AppAggregateEntity
{
    private SystemNotification() : base(Guid.NewGuid())
    {
        Title = string.Empty;
        RequiredPermission = string.Empty;
    }

    internal SystemNotification(
        SystemNotificationType type,
        SystemNotificationSeverity severity,
        string title,
        string? message,
        string requiredPermission,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? actionUrl,
        Guid? createdByUserId) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredPermission);
        if (!string.IsNullOrWhiteSpace(actionUrl) && !actionUrl.StartsWith('/'))
            throw new ArgumentException("Action URL must be an internal relative URL.", nameof(actionUrl));

        Type = type;
        Severity = severity;
        Title = title;
        Message = message;
        RequiredPermission = requiredPermission;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        ActionUrl = actionUrl;
        CreatedByUserId = createdByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public SystemNotificationType Type { get; private set; }
    public SystemNotificationSeverity Severity { get; private set; }
    public string Title { get; private set; }
    public string? Message { get; private set; }
    public string RequiredPermission { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? ActionUrl { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
}
