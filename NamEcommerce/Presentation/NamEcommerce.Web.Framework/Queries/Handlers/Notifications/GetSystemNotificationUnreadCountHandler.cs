using MediatR;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Web.Contracts.Queries.Models.Notifications;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Notifications;

public sealed class GetSystemNotificationUnreadCountHandler(ISystemNotificationAppService appService)
    : IRequestHandler<GetSystemNotificationUnreadCountQuery, int>
{
    public Task<int> Handle(GetSystemNotificationUnreadCountQuery request, CancellationToken cancellationToken)
        => appService.CountUnreadAsync(request.UserId, request.UserPermissions);
}
