using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Models.OrderFulfillment;
using NamEcommerce.Web.Services.OrderFulfillment;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = SystemPermissions.Orders.View)]
public sealed class OrderFulfillmentController(
    IMediator mediator, IOrderFulfillmentModelFactory orderFulfillmentModelFactory,
    IOrderFulfillmentScheduleAppService orderFulfillmentScheduleAppService) : BaseAuthorizedController
{
    public async Task<IActionResult> Index(OrderFulfillmentBoardSearchModel search)
    {
        var model = await orderFulfillmentModelFactory.PrepareBoardModelAsync(search);
        return View(model);
    }

    public async Task<IActionResult> OrderSchedules(Guid orderId)
    {
        var model = await orderFulfillmentModelFactory.PrepareSchedulePanelModelAsync(orderId);
        return PartialView("~/Views/Order/_OrderFulfillmentSchedulePanel.cshtml", model);
    }

    public async Task<IActionResult> TopbarSchedule()
    {
        var model = await orderFulfillmentModelFactory.PrepareBoardModelAsync(new OrderFulfillmentBoardSearchModel
        {
            Date = DateTime.Today
        });

        return PartialView("~/Views/OrderFulfillment/_TopbarSchedulePanel.cshtml", model.Board);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
    public async Task<IActionResult> CreateSchedule(OrderFulfillmentScheduleInputModel model)
    {
        if (!ModelState.IsValid)
            return this.JsonError(GetErrorMessage());

        var result = await mediator.Send(new CreateOrderFulfillmentScheduleCommand
        {
            OrderId = model.OrderId,
            ScheduledFromUtc = model.ScheduledFromUtc,
            ScheduledToUtc = model.ScheduledToUtc,
            Mode = model.Mode,
            Note = model.Note,
            Items = model.Items.Select(ToCommandItem).ToList()
        });

        if (result.Success)
            return this.JsonOk(message: Localizer["Msg.SaveSuccess"]);
        return this.JsonError(result.ErrorMessage!);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
    public async Task<IActionResult> UpdateSchedule(OrderFulfillmentScheduleInputModel model)
    {
        if (!ModelState.IsValid)
            return this.JsonError(GetErrorMessage());
        if (!model.Id.HasValue)
            return this.JsonError(LocalizeError("Error.OrderFulfillmentScheduleIsNotFound"));

        var schedule = await orderFulfillmentScheduleAppService.GetByIdAsync(model.Id.Value);
        if (schedule is null)
            return this.JsonError(LocalizeError("Error.OrderFulfillmentScheduleIsNotFound"));

        var result = await mediator.Send(new UpdateOrderFulfillmentScheduleCommand
        {
            Id = model.Id.Value,
            OrderId = model.OrderId,
            ScheduledFromUtc = model.ScheduledFromUtc,
            ScheduledToUtc = model.ScheduledToUtc,
            Mode = model.Mode,
            Note = model.Note,
            Items = model.Items.Select(ToCommandItem).ToList()
        });

        if (result.Success)
            return this.JsonOk(message: Localizer["Msg.SaveSuccess"]);
        return this.JsonError(result.ErrorMessage!);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
    public async Task<IActionResult> SetScheduleActive(Guid id, bool isActive)
    {
        var result = await mediator.Send(new SetOrderFulfillmentScheduleActiveCommand(id, isActive));

        if (result.Success)
            return this.JsonOk(message: Localizer["Msg.SaveSuccess"]);
        return this.JsonError(result.ErrorMessage!);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var result = await mediator.Send(new DeleteOrderFulfillmentScheduleCommand(id));

        if (result.Success)
            return this.JsonOk(message: Localizer["Msg.SaveSuccess"]);
        return this.JsonError(result.ErrorMessage!);
    }

    private static OrderFulfillmentScheduleItemCommand ToCommandItem(OrderFulfillmentScheduleItemInputModel item)
        => new()
        {
            OrderItemId = item.OrderItemId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity
        };
}
