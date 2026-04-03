using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.Orders;

namespace NamEcommerce.Web.Controllers;

public sealed class OrderController : BaseAuthorizedController
{
    private readonly IMediator _mediator;

    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
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

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(string? keywords, int? status, int pageNumber = 1)
    {
        var model = await _mediator.Send(new NamEcommerce.Web.Contracts.Queries.Models.Orders.GetOrderListQuery
        {
            Keywords = keywords,
            Status = status,
            PageIndex = pageNumber - 1,
            PageSize = 20
        });
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var warehouses = await _mediator.Send(new GetWarehouseListQuery { Keywords = null, PageIndex = 0, PageSize = 100 });
        ViewBag.AvailableWarehouses = warehouses.Data.Items;

        var model = new CreateOrderModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            var warehouses = await _mediator.Send(new GetWarehouseListQuery { Keywords = null, PageIndex = 0, PageSize = 100 });
            ViewBag.AvailableWarehouses = warehouses.Data.Items;
            return View(model);
        }

        var result = await _mediator.Send(new CreateOrderCommand
        {
            CustomerId = model.CustomerId ?? Guid.Empty,
            WarehouseId = model.WarehouseId,
            OrderDiscount = model.OrderDiscount ?? 0,
            Note = model.Note,
            Items = model.Items.Select(i => new NamEcommerce.Web.Contracts.Models.Orders.OrderItemModel(i.ProductId ?? Guid.Empty, i.Quantity ?? 0, i.UnitPrice ?? 0)).ToList()
        });

        if (!result.Success)
        {
            var warehouses = await _mediator.Send(new GetWarehouseListQuery { Keywords = null, PageIndex = 0, PageSize = 100 });
            ViewBag.AvailableWarehouses = warehouses.Data.Items;
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Error");
            return View(model);
        }

        TempData["OrderSuccessMessage"] = "Order created";
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var dto = await _mediator.Send(new NamEcommerce.Web.Contracts.Queries.Models.Orders.GetOrderByIdQuery { Id = id });
        if (dto == null)
        {
            TempData["OrderErrorMessage"] = "Order not found";
            return RedirectToAction(nameof(List));
        }

        var model = new NamEcommerce.Web.Models.Orders.OrderDetailsModel
        {
            Id = dto.Id,
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            TotalAmount = dto.TotalAmount,
            OrderDiscount = dto.OrderDiscount,
            Status = dto.Status,
            Note = dto.Note
        };
        foreach (var it in dto.Items)
            model.Items.Add(new NamEcommerce.Web.Models.Orders.OrderDetailsItemModel 
            { 
                Id = it.ItemId, 
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
}

