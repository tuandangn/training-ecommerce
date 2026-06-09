using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Notifications;

[Serializable]
public sealed record SystemNotificationAppDto
{
    public required Guid Id { get; init; }
    public required int Type { get; init; }
    public required int Severity { get; init; }
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
public sealed record CreateSystemNotificationAppDto
{
    public required int Type { get; init; }
    public required int Severity { get; init; }
    public required string Title { get; init; }
    public string? Message { get; init; }
    public required string RequiredPermission { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public string? ActionUrl { get; init; }
    public Guid? CreatedByUserId { get; init; }

    public (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
            return (false, "Error.SystemNotificationTitleRequired");
        if (string.IsNullOrWhiteSpace(RequiredPermission))
            return (false, "Error.SystemNotificationPermissionRequired");
        if (!string.IsNullOrWhiteSpace(ActionUrl) && !ActionUrl.StartsWith('/'))
            return (false, "Error.SystemNotificationActionUrlInvalid");

        return (true, null);
    }
}

[Serializable]
public sealed record SystemNotificationListFilterAppDto
{
    public IReadOnlyCollection<string> UserPermissions { get; init; } = [];
    public Guid? UserId { get; init; }
    public int? Type { get; init; }
    public int? Severity { get; init; }
    public bool? IsRead { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; } = 20;

    public (bool valid, string? errorMessage) Validate()
    {
        if (PageIndex < 0)
            return (false, "Error.PaginationPageIndexInvalid");
        if (PageSize <= 0)
            return (false, "Error.PaginationPageSizeInvalid");

        return (true, null);
    }
}

[Serializable]
public sealed record CreateSystemNotificationResultAppDto : CommonActionResultDto
{
    public Guid? CreatedId { get; init; }
    public SystemNotificationAppDto? Notification { get; init; }
}
