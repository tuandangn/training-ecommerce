using Microsoft.AspNetCore.SignalR;
using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Web.Hubs.Notifications;

namespace NamEcommerce.Web.Services.Notifications;

public sealed class SignalRSystemNotificationRealtimePublisher(
    IHubContext<SystemNotificationHub> hubContext,
    ILogger<SignalRSystemNotificationRealtimePublisher> logger) : ISystemNotificationRealtimePublisher
{
    public async Task PublishCreatedAsync(SystemNotificationAppDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await hubContext.Clients
                .Group(SystemNotificationHub.PermissionGroup(notification.RequiredPermission))
                .SendAsync(SystemNotificationHub.NotificationCreatedMethod, notification, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish system notification {NotificationId}.", notification.Id);
        }
    }
}
