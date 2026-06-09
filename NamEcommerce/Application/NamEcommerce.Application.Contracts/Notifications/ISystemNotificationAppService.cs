using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Notifications;

namespace NamEcommerce.Application.Contracts.Notifications;

public interface ISystemNotificationAppService
{
    Task<CreateSystemNotificationResultAppDto> CreateAsync(CreateSystemNotificationAppDto dto);
    Task<IPagedDataAppDto<SystemNotificationAppDto>> GetNotificationsAsync(SystemNotificationListFilterAppDto dto);
    Task<int> CountUnreadAsync(Guid userId, IReadOnlyCollection<string> userPermissions);
    Task<CommonActionResultDto> MarkReadAsync(Guid notificationId, Guid userId);
    Task<CommonActionResultDto> MarkAllReadAsync(Guid userId, IReadOnlyCollection<string> userPermissions);
}
