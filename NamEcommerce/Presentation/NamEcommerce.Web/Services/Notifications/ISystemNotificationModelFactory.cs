using NamEcommerce.Web.Contracts.Models.Notifications;

namespace NamEcommerce.Web.Services.Notifications;

public interface ISystemNotificationModelFactory
{
    Task<SystemNotificationListModel> PrepareSystemNotificationListModelAsync(SystemNotificationSearchModel searchModel);
    Task<UserNotificationPermissionSnapshot> PreparePermissionSnapshotAsync(CancellationToken cancellationToken = default);
}
