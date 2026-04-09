using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Services.Orders;

namespace NamEcommerce.Web.Controllers;

public sealed class OrderController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IOrderModelFactory _orderModelFactory;

    public OrderController(IMediator mediator, IOrderModelFactory orderModelFactory)
    {
        _mediator = mediator;
        _orderModelFactory = orderModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(OrderListSearchModel search)
    {
        var model = await _orderModelFactory.PrepareOrderListModel(search);

        return View(model);
    }

    public IActionResult Create()
    {
        var model = new CreateOrderModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        if(model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn hàng hóa.");
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        var result = await _mediator.Send(new CreateOrderCommand
        {
            CustomerId = model.CustomerId!.Value,
            OrderDiscount = model.OrderDiscount,
            Note = model.Note,
            ExpectedShippingDate = model.ExpectedShippingDate!.Value,
            Items = model.Items.Select(item => new OrderItemModel
            {
                ProductId = item.ProductId ?? default,
                Quantity = item.Quantity ?? default,
                UnitPrice = item.UnitPrice ?? default
            }).ToList()
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var dto = await _mediator.Send(new GetOrderByIdQuery { Id = id });
        if (dto == null)
        {
            TempData["OrderErrorMessage"] = "Order not found";
            return RedirectToAction(nameof(List));
        }

        var model = new OrderDetailsModel
        {
            Id = dto.Id,
            Code = dto.Code,
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            TotalAmount = dto.TotalAmount,
            OrderDiscount = dto.OrderDiscount,
            Status = dto.Status,
            Note = dto.Note,
            PaymentStatus = dto.PaymentStatus,
            PaymentMethod = dto.PaymentMethod,
            PaidOnUtc = dto.PaidOnUtc,
            PaymentNote = dto.PaymentNote,
            ShippingStatus = dto.ShippingStatus,
            ShippingAddress = dto.ShippingAddress,
            ShippedOnUtc = dto.ShippedOnUtc,
            ShippingNote = dto.ShippingNote,
            CancellationReason = dto.CancellationReason
        };
        foreach (var it in dto.Items)
            model.Items.Add(new OrderDetailsItemModel
            {
                ItemId = it.ItemId,
                ProductId = it.ProductId,
                ProductName = it.ProductName,
                Quantity = it.Quantity,
                UnitPrice = it.UnitPrice
            });

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrder(UpdateOrderCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> AddItem(Guid orderId, Guid productId, decimal quantity, decimal unitPrice)
    {
        var result = await _mediator.Send(new AddOrderItemCommand
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        });

        if (!result.Success)
            TempData["OrderErrorMessage"] = result.ErrorMessage;
        else
            TempData["OrderSuccessMessage"] = "Item added";

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(Guid orderId, int status)
    {
        var result = await _mediator.Send(new ChangeOrderStatusCommand { OrderId = orderId, Status = status });
        if (!result.Success)
            TempData["OrderErrorMessage"] = result.ErrorMessage;
        else
            TempData["OrderSuccessMessage"] = "Status changed";

        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateItem(UpdateOrderItemCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveItem(DeleteOrderItemCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsPaid(MarkOrderAsPaidCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            TempData["OrderErrorMessage"] = result.ErrorMessage;
        else
            TempData["OrderSuccessMessage"] = "Payment recorded successfully.";

        return RedirectToAction(nameof(Details), new { id = command.OrderId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateShipping(UpdateOrderShippingCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            TempData["OrderErrorMessage"] = result.ErrorMessage;
        else
            TempData["OrderSuccessMessage"] = "Shipping status updated successfully.";

        return RedirectToAction(nameof(Details), new { id = command.OrderId });
    }

    [HttpPost]
    public async Task<IActionResult> CancelOrder(CancelOrderCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            TempData["OrderErrorMessage"] = result.ErrorMessage;
        else
            TempData["OrderSuccessMessage"] = "Order has been cancelled.";

        return RedirectToAction(nameof(Details), new { id = command.OrderId });
    }
}

