using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Web.Contracts.Commands.Models.Notifications;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Notifications;

public sealed class MarkSystemNotificationReadHandler(ISystemNotificationAppService appService)
    : IRequestHandler<MarkSystemNotificationReadCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(MarkSystemNotificationReadCommand request, CancellationToken cancellationToken)
        => SystemNotificationActionResultMapper.Map(
            await appService.MarkReadAsync(request.NotificationId, request.UserId).ConfigureAwait(false));
}

public sealed class MarkAllSystemNotificationsReadHandler(ISystemNotificationAppService appService)
    : IRequestHandler<MarkAllSystemNotificationsReadCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(MarkAllSystemNotificationsReadCommand request, CancellationToken cancellationToken)
        => SystemNotificationActionResultMapper.Map(
            await appService.MarkAllReadAsync(request.UserId, request.UserPermissions).ConfigureAwait(false));
}

file static class SystemNotificationActionResultMapper
{
    public static CommonActionResultModel Map(CommonActionResultDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            SuccessMessage = result.Success ? "Msg.SaveSuccess" : null
        };
}
