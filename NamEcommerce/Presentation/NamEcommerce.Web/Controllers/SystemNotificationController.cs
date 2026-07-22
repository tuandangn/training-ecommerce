using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Notifications;
using NamEcommerce.Web.Contracts.Models.Notifications;
using NamEcommerce.Web.Contracts.Queries.Models.Notifications;
using NamEcommerce.Web.Services.Notifications;

namespace NamEcommerce.Web.Controllers;

public sealed class SystemNotificationController(ISystemNotificationModelFactory systemNotificationModelFactory, IMediator mediator) 
    : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(SystemNotificationSearchModel searchModel)
    {
        var model = await systemNotificationModelFactory
            .PrepareSystemNotificationListModelAsync(searchModel)
            .ConfigureAwait(false);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var snapshot = await systemNotificationModelFactory.PreparePermissionSnapshotAsync().ConfigureAwait(false);
        if (!snapshot.UserId.HasValue)
            return Json(new { count = 0 });

        var count = await mediator.Send(new GetSystemNotificationUnreadCountQuery(
            snapshot.UserId.Value,
            snapshot.Permissions)).ConfigureAwait(false);

        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> Latest()
    {
        var snapshot = await systemNotificationModelFactory.PreparePermissionSnapshotAsync().ConfigureAwait(false);
        if (!snapshot.UserId.HasValue)
            return Json(new { items = Array.Empty<object>() });

        var model = await mediator.Send(new GetSystemNotificationListQuery
        {
            UserId = snapshot.UserId,
            UserPermissions = snapshot.Permissions,
            PageIndex = 0,
            PageSize = 5
        }).ConfigureAwait(false);

        var items = model.Data.Items.Select(item => new
        {
            item.Id,
            item.Type,
            item.Severity,
            item.Title,
            item.Message,
            item.ActionUrl,
            item.IsRead,
            CreatedOn = item.CreatedOn.ToString("dd/MM HH:mm"),
            OpenUrl = Url.Action(nameof(Open), new { id = item.Id, actionUrl = item.ActionUrl })
        });

        return Json(new { items });
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var snapshot = await systemNotificationModelFactory.PreparePermissionSnapshotAsync().ConfigureAwait(false);
        if (!snapshot.UserId.HasValue)
            return Forbid();

        var result = await mediator.Send(new MarkSystemNotificationReadCommand(id, snapshot.UserId.Value))
            .ConfigureAwait(false);
        if (!result.Success)
            NotifyError(result.ErrorMessage ?? "Error.SystemNotificationCannotMarkRead");

        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        var snapshot = await systemNotificationModelFactory.PreparePermissionSnapshotAsync().ConfigureAwait(false);
        if (!snapshot.UserId.HasValue)
            return Forbid();

        var result = await mediator.Send(new MarkAllSystemNotificationsReadCommand(snapshot.UserId.Value, snapshot.Permissions))
            .ConfigureAwait(false);
        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.SystemNotificationCannotMarkRead");

        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Open(Guid id, string? actionUrl)
    {
        var snapshot = await systemNotificationModelFactory.PreparePermissionSnapshotAsync().ConfigureAwait(false);
        if (!snapshot.UserId.HasValue)
            return Forbid();

        var result = await mediator.Send(new MarkSystemNotificationReadCommand(id, snapshot.UserId.Value))
            .ConfigureAwait(false);
        if (!result.Success)
            NotifyError(result.ErrorMessage ?? "Error.SystemNotificationCannotMarkRead");

        if (!string.IsNullOrWhiteSpace(actionUrl) && Url.IsLocalUrl(actionUrl))
            return Redirect(actionUrl);

        return RedirectToAction(nameof(List));
    }
}
