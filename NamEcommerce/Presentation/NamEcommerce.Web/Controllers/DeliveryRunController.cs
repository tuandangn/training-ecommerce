using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Services.DeliveryNotes;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = SystemPermissions.DeliveryRuns.View)]
public sealed class DeliveryRunController(
    IDeliveryRunModelFactory deliveryRunModelFactory,
    IMediator mediator) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(DeliveryRunSearchModel searchModel)
    {
        var model = await deliveryRunModelFactory.PrepareDeliveryRunListModelAsync(searchModel).ConfigureAwait(false);
        return View(model);
    }

    [Authorize(Policy = SystemPermissions.DeliveryRuns.Manage)]
    public async Task<IActionResult> Create(Guid? assignedDeliveryUserId = null)
    {
        var model = await deliveryRunModelFactory.PrepareCreateDeliveryRunModelAsync(new CreateDeliveryRunModel
        {
            AssignedDeliveryUserId = assignedDeliveryUserId ?? Guid.Empty
        }).ConfigureAwait(false);

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryRuns.Manage)]
    public async Task<IActionResult> Create(CreateDeliveryRunModel model)
    {
        if (model.DeliveryNoteIds.Count == 0)
            AddLocalizedModelError("Error.DeliveryRunItemsRequired");

        if (!ModelState.IsValid)
        {
            model = await deliveryRunModelFactory.PrepareCreateDeliveryRunModelAsync(model).ConfigureAwait(false);
            return View(model);
        }

        var result = await mediator.Send(new CreateDeliveryRunCommand
        {
            AssignedDeliveryUserId = model.AssignedDeliveryUserId,
            DeliveryNoteIds = model.DeliveryNoteIds,
            Note = model.Note
        }).ConfigureAwait(false);

        if (result.Success)
        {
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
            return RedirectToAction(nameof(Details), new { id = result.CreatedId });
        }

        AddLocalizedModelError(result.ErrorMessage ?? "Error.DeliveryRunCreateFailed");
        model = await deliveryRunModelFactory.PrepareCreateDeliveryRunModelAsync(model).ConfigureAwait(false);
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var model = await deliveryRunModelFactory.PrepareDeliveryRunDetailsModelAsync(id).ConfigureAwait(false);
            return View(model);
        }
        catch
        {
            NotifyError("Error.DeliveryRunNotFound");
            return RedirectToAction(nameof(List));
        }
    }

    [Authorize(Policy = SystemPermissions.DeliveryRuns.Manage)]
    public async Task<IActionResult> Print(Guid id)
    {
        try
        {
            var model = await deliveryRunModelFactory.PrepareDeliveryRunDetailsModelAsync(id).ConfigureAwait(false);
            return View(model);
        }
        catch
        {
            NotifyError("Error.DeliveryRunNotFound");
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryRuns.Manage)]
    public async Task<IActionResult> IssuePaperManifest(Guid id)
    {
        var result = await mediator.Send(new IssuePaperManifestDeliveryRunCommand(id)).ConfigureAwait(false);
        if (result.Success)
            return RedirectToAction(nameof(Print), new { id });

        NotifyError(result.ErrorMessage ?? "Error.DeliveryRunCannotIssueManifest");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryRuns.Manage)]
    public async Task<IActionResult> HandOver(Guid id)
    {
        var result = await mediator.Send(new HandOverDeliveryRunCommand(id)).ConfigureAwait(false);
        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.DeliveryRunCannotHandOver");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryRuns.Manage)]
    public async Task<IActionResult> Close(Guid id)
    {
        var result = await mediator.Send(new CloseDeliveryRunCommand(id)).ConfigureAwait(false);
        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.DeliveryRunCannotClose");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryRuns.ConfirmCashHandover)]
    public async Task<IActionResult> ConfirmCashHandover(Guid id, decimal amount, string? note)
    {
        var result = await mediator.Send(new ConfirmDeliveryRunCashHandoverCommand(id, amount, note)).ConfigureAwait(false);
        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.DeliveryRunCannotConfirmCashHandover");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryRuns.Manage)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await mediator.Send(new CancelDeliveryRunCommand(id)).ConfigureAwait(false);
        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.DeliveryRunCannotCancel");

        return RedirectToAction(nameof(Details), new { id });
    }
}
