using MediatR;

namespace NamEcommerce.Web.Contracts.Queries.Models.Notifications;

[Serializable]
public sealed record GetSystemNotificationUnreadCountQuery(
    Guid UserId,
    IReadOnlyCollection<string> UserPermissions) : IRequest<int>;
