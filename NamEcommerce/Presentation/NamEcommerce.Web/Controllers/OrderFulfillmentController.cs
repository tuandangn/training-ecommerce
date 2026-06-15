using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.OrderFulfillment;
using NamEcommerce.Web.Services.OrderFulfillment;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = SystemPermissions.Orders.View)]
public sealed class OrderFulfillmentController(
    IMediator mediator,
    IOrderFulfillmentModelFactory orderFulfillmentModelFactory) : BaseAuthorizedController
{
    public async Task<IActionResult> Index(OrderFulfillmentBoardSearchModel search)
    {
        var model = await orderFulfillmentModelFactory.PrepareBoardModelAsync(search).ConfigureAwait(false);
        return View(model);
    }

    public async Task<IActionResult> OrderSchedules(Guid orderId)
    {
        var model = await orderFulfillmentModelFactory.PrepareSchedulePanelModelAsync(orderId).ConfigureAwait(false);
        return PartialView("~/Views/Order/_OrderFulfillmentSchedulePanel.cshtml", model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
    public async Task<IActionResult> CreateSchedule(OrderFulfillmentScheduleInputModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var result = await mediator.Send(new CreateOrderFulfillmentScheduleCommand
        {
            OrderId = model.OrderId,
            ScheduledFromUtc = model.ScheduledFromUtc,
            ScheduledToUtc = model.ScheduledToUtc,
            Mode = model.Mode,
            Note = model.Note,
            Items = model.Items.Select(ToCommandItem).ToList()
        }).ConfigureAwait(false);

        return ToJson(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
    public async Task<IActionResult> UpdateSchedule(OrderFulfillmentScheduleInputModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });
        if (!model.Id.HasValue)
            return Json(new { success = false, message = LocalizeError("Error.OrderFulfillmentScheduleIsNotFound") });

        var result = await mediator.Send(new UpdateOrderFulfillmentScheduleCommand
        {
            Id = model.Id.Value,
            OrderId = model.OrderId,
            ScheduledFromUtc = model.ScheduledFromUtc,
            ScheduledToUtc = model.ScheduledToUtc,
            Mode = model.Mode,
            Note = model.Note,
            Items = model.Items.Select(ToCommandItem).ToList()
        }).ConfigureAwait(false);

        return ToJson(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
    public async Task<IActionResult> SetScheduleActive(Guid id, bool isActive)
    {
        var result = await mediator.Send(new SetOrderFulfillmentScheduleActiveCommand(id, isActive)).ConfigureAwait(false);
        return ToJson(result);
    }

    private IActionResult ToJson(CommonActionResultModel result)
        => Json(result.Success
            ? new { success = true, message = LocalizeError(result.SuccessMessage ?? "Msg.SaveSuccess") }
            : new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.InvalidRequest") });

    private static OrderFulfillmentScheduleItemCommand ToCommandItem(OrderFulfillmentScheduleItemInputModel item)
        => new()
        {
            OrderItemId = item.OrderItemId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity
        };
}
