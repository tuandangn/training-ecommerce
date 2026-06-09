using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Notifications;

namespace NamEcommerce.Domain.Shared.Services.Notifications;

public interface ISystemNotificationManager
{
    Task<SystemNotificationDto> CreateAsync(CreateSystemNotificationDto dto);
    Task<IPagedDataDto<SystemNotificationDto>> GetNotificationsAsync(SystemNotificationListFilterDto dto);
    Task<int> CountUnreadAsync(Guid userId, IReadOnlyCollection<string> userPermissions);
    Task MarkReadAsync(Guid notificationId, Guid userId);
    Task MarkAllReadAsync(Guid userId, IReadOnlyCollection<string> userPermissions);
}
