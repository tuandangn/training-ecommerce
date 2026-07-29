using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Notifications;
using NamEcommerce.Web.Contracts.Queries.Models.Notifications;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Notifications;

public sealed class GetSystemNotificationListHandler(ISystemNotificationAppService appService)
    : IRequestHandler<GetSystemNotificationListQuery, SystemNotificationListModel>
{
    private static readonly IReadOnlyDictionary<int, IReadOnlyCollection<int>> TypesByGroup =
        new Dictionary<int, IReadOnlyCollection<int>>
        {
            [(int)SystemNotificationTypeGroup.CustomerPortal] = [10, 20, 30, 40],
            [(int)SystemNotificationTypeGroup.Delivery] = [100, 110, 120, 130, 200, 210, 220, 601],
            [(int)SystemNotificationTypeGroup.Procurement] = [300, 400, 410],
            [(int)SystemNotificationTypeGroup.Inventory] = [500]
        };

    public async Task<SystemNotificationListModel> Handle(GetSystemNotificationListQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<int>? types = null;
        if (request.TypeGroup.HasValue)
            TypesByGroup.TryGetValue(request.TypeGroup.Value, out types);

        var result = await appService.GetNotificationsAsync(new SystemNotificationListFilterAppDto
        {
            UserPermissions = request.UserPermissions,
            UserId = request.UserId,
            Type = types is null ? request.Type : null,
            Types = types,
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
            CreatedOn = DateTimeHelper.ToLocalTime(item.CreatedOnUtc),
            ReadOn = DateTimeHelper.ToLocalTime(item.ReadOnUtc)
        }).ToList();

        return new SystemNotificationListModel
        {
            UserPermissions = request.UserPermissions,
            UserId = request.UserId,
            Type = request.Type,
            TypeGroup = request.TypeGroup,
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
