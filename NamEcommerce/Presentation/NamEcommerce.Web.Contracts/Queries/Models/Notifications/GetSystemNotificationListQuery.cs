using MediatR;
using NamEcommerce.Web.Contracts.Models.Notifications;

namespace NamEcommerce.Web.Contracts.Queries.Models.Notifications;

[Serializable]
public sealed class GetSystemNotificationListQuery : IRequest<SystemNotificationListModel>
{
    public IReadOnlyCollection<string> UserPermissions { get; init; } = [];
    public Guid? UserId { get; init; }
    public int? Type { get; init; }
    public int? TypeGroup { get; init; }
    public int? Severity { get; init; }
    public bool? IsRead { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; } = 20;
}
