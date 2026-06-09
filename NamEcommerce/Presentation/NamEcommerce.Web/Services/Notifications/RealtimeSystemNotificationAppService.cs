using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Services.Notifications;

namespace NamEcommerce.Web.Services.Notifications;

public sealed class RealtimeSystemNotificationAppService(
    SystemNotificationAppService inner,
    ISystemNotificationRealtimePublisher publisher) : ISystemNotificationAppService
{
    public async Task<CreateSystemNotificationResultAppDto> CreateAsync(CreateSystemNotificationAppDto dto)
    {
        var result = await inner.CreateAsync(dto).ConfigureAwait(false);
        if (result.Success && result.Notification is not null)
            await publisher.PublishCreatedAsync(result.Notification).ConfigureAwait(false);

        return result;
    }

    public Task<IPagedDataAppDto<SystemNotificationAppDto>> GetNotificationsAsync(SystemNotificationListFilterAppDto dto)
        => inner.GetNotificationsAsync(dto);

    public Task<int> CountUnreadAsync(Guid userId, IReadOnlyCollection<string> userPermissions)
        => inner.CountUnreadAsync(userId, userPermissions);

    public Task<CommonActionResultDto> MarkReadAsync(Guid notificationId, Guid userId)
        => inner.MarkReadAsync(notificationId, userId);

    public Task<CommonActionResultDto> MarkAllReadAsync(Guid userId, IReadOnlyCollection<string> userPermissions)
        => inner.MarkAllReadAsync(userId, userPermissions);
}
