using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Enums.Notifications;

namespace NamEcommerce.Domain.Shared.Dtos.Notifications;

[Serializable]
public sealed record CreateSystemNotificationDto
{
    public required SystemNotificationType Type { get; init; }
    public required SystemNotificationSeverity Severity { get; init; }
    public required string Title { get; init; }
    public string? Message { get; init; }
    public required string RequiredPermission { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? ActionUrl { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("Title is required.", nameof(Title));
        if (string.IsNullOrWhiteSpace(RequiredPermission))
            throw new ArgumentException("RequiredPermission is required.", nameof(RequiredPermission));
        if (!string.IsNullOrWhiteSpace(ActionUrl) && !ActionUrl.StartsWith('/'))
            throw new ArgumentException("ActionUrl must be a relative internal URL.", nameof(ActionUrl));
    }
}

[Serializable]
public sealed record SystemNotificationDto(Guid Id)
{
    public required SystemNotificationType Type { get; init; }
    public required SystemNotificationSeverity Severity { get; init; }
    public required string Title { get; init; }
    public string? Message { get; init; }
    public required string RequiredPermission { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? ActionUrl { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? ReadOnUtc { get; init; }
}

[Serializable]
public sealed record SystemNotificationListFilterDto
{
    public IReadOnlyCollection<string> UserPermissions { get; init; } = [];
    public Guid? UserId { get; init; }
    public SystemNotificationType? Type { get; init; }
    public IReadOnlyCollection<SystemNotificationType>? Types { get; init; }
    public SystemNotificationSeverity? Severity { get; init; }
    public bool? IsRead { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; } = 20;
}
