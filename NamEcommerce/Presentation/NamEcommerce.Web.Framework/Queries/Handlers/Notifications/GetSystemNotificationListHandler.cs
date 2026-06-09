using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Notifications;
using NamEcommerce.Web.Contracts.Queries.Models.Notifications;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Notifications;

public sealed class GetSystemNotificationListHandler(ISystemNotificationAppService appService)
    : IRequestHandler<GetSystemNotificationListQuery, SystemNotificationListModel>
{
    public async Task<SystemNotificationListModel> Handle(GetSystemNotificationListQuery request, CancellationToken cancellationToken)
    {
        var result = await appService.GetNotificationsAsync(new SystemNotificationListFilterAppDto
        {
            UserPermissions = request.UserPermissions,
            UserId = request.UserId,
            Type = request.Type,
            Severity = request.Severity,
            IsRead = request.IsRead,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        }).ConfigureAwait(false);

        var items = result.Items.Select(item => new SystemNotificationListItemModel
        {
            Id = item.Id,
            Type = item.Type,
            Severity = item.Severity,
            Title = item.Title,
            Message = item.Message,
            RequiredPermission = item.RequiredPermission,
            RelatedEntityType = item.RelatedEntityType,
            RelatedEntityId = item.RelatedEntityId,
            ActionUrl = item.ActionUrl,
            CreatedByUserId = item.CreatedByUserId,
            CreatedOn = item.CreatedOnUtc.ToLocalTime(),
            ReadOn = item.ReadOnUtc?.ToLocalTime()
        }).ToList();

        return new SystemNotificationListModel
        {
            UserPermissions = request.UserPermissions,
            UserId = request.UserId,
            Type = request.Type,
            Severity = request.Severity,
            IsRead = request.IsRead,
            Data = PagedDataModel.Create(
                items,
                result.Pagination.PageIndex,
                result.Pagination.PageSize,
                result.Pagination.TotalCount)
        };
    }
}
