using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Notifications;

[Serializable]
public sealed class SystemNotificationSearchModel
{
    public int? Type { get; set; }
    public int? Severity { get; set; }
    public bool? IsRead { get; set; }
    public int? TypeGroup { get; set; }
    public int? PageNumber { get; set; }
    public int? PageSize { get; set; }
}

[Serializable]
public sealed class SystemNotificationListModel
{
    public IReadOnlyCollection<string> UserPermissions { get; init; } = [];
    public Guid? UserId { get; init; }
    public int? Type { get; init; }
    public int? Severity { get; init; }
    public bool? IsRead { get; init; }
    public int? TypeGroup { get; init; }
    public required IPagedDataModel<SystemNotificationListItemModel> Data { get; init; }
}

[Serializable]
public sealed class SystemNotificationListItemModel
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
    public required DateTime CreatedOn { get; init; }
    public DateTime? ReadOn { get; init; }
    public bool IsRead => ReadOn.HasValue;
}
