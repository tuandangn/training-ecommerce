using MediatR;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Notifications;
using NamEcommerce.Web.Contracts.Queries.Models.Notifications;

namespace NamEcommerce.Web.Services.Notifications;

public sealed class SystemNotificationModelFactory(
    IMediator mediator,
    AppConfig appConfig,
    IUserNotificationPermissionService permissionService) : ISystemNotificationModelFactory
{
    public async Task<SystemNotificationListModel> PrepareSystemNotificationListModelAsync(SystemNotificationSearchModel searchModel)
    {
        var pageNumber = searchModel.PageNumber ?? 1;
        var pageSize = searchModel.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || !appConfig.PageSizeOptions.Contains(pageSize)) pageSize = appConfig.DefaultPageSize;

        var snapshot = await permissionService.GetCurrentAsync().ConfigureAwait(false);
        return await mediator.Send(new GetSystemNotificationListQuery
        {
            UserId = snapshot.UserId,
            UserPermissions = snapshot.Permissions,
            Type = searchModel.Type,
            TypeGroup = searchModel.TypeGroup,
            Severity = searchModel.Severity,
            IsRead = searchModel.IsRead,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        }).ConfigureAwait(false);
    }

    public Task<UserNotificationPermissionSnapshot> PreparePermissionSnapshotAsync(CancellationToken cancellationToken = default)
        => permissionService.GetCurrentAsync(cancellationToken);
}
