using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NamEcommerce.Web.Services.Notifications;

namespace NamEcommerce.Web.Hubs.Notifications;

[Authorize]
public sealed class SystemNotificationHub(IUserNotificationPermissionService permissionService) : Hub
{
    public const string Route = "/hubs/system-notifications";
    public const string NotificationCreatedMethod = "systemNotificationCreated";

    public static string PermissionGroup(string permission)
        => $"permission:{permission.ToUpperInvariant()}";

    public override async Task OnConnectedAsync()
    {
        var snapshot = await permissionService.GetForUserAsync(Context.User, Context.ConnectionAborted)
            .ConfigureAwait(false);

        foreach (var permission in snapshot.Permissions)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, PermissionGroup(permission), Context.ConnectionAborted)
                .ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }
}
