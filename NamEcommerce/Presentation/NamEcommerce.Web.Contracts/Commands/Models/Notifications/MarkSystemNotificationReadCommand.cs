using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Notifications;

[Serializable]
public sealed record MarkSystemNotificationReadCommand(Guid NotificationId, Guid UserId)
    : ICommand<CommonActionResultModel>;
