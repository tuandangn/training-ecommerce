using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Notifications;

[Serializable]
public sealed record MarkAllSystemNotificationsReadCommand(
    Guid UserId,
    IReadOnlyCollection<string> UserPermissions) : IRequest<CommonActionResultModel>;
