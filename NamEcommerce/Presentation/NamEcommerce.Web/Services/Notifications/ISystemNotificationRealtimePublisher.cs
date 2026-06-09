using NamEcommerce.Application.Contracts.Dtos.Notifications;

namespace NamEcommerce.Web.Services.Notifications;

public interface ISystemNotificationRealtimePublisher
{
    Task PublishCreatedAsync(SystemNotificationAppDto notification, CancellationToken cancellationToken = default);
}
