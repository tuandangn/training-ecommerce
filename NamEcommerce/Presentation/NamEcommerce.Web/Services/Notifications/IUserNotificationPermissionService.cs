using System.Security.Claims;

namespace NamEcommerce.Web.Services.Notifications;

public sealed record UserNotificationPermissionSnapshot(Guid? UserId, IReadOnlyCollection<string> Permissions);

public interface IUserNotificationPermissionService
{
    Task<UserNotificationPermissionSnapshot> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<UserNotificationPermissionSnapshot> GetForUserAsync(ClaimsPrincipal? user, CancellationToken cancellationToken = default);
}
