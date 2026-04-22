using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Extensions;
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

        if (model.Items.Count == 0)
        {
            AddLocalizedModelError("Error.OrderItemRequired");
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        var orderSubTotal = model.Items.Sum(item => item.ItemSubTotal);
        if ((model.OrderDiscount ?? 0) > orderSubTotal)
        {
            ModelState.AddModelError(nameof(model.OrderDiscount), LocalizeError("Error.OrderDiscountExceedsTotal"));
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        var result = await _mediator.Send(new CreateOrderCommand
        {
            CustomerId = model.CustomerId!.Value,
            OrderDiscount = model.OrderDiscount,
            Note = model.Note,
            ExpectedShippingDate = model.ExpectedShippingDate,
            ShippingAddress = model.ShippingAddress!,
            Items = model.Items.Select(item => new OrderItemModel
            {
                ProductId = item.ProductId ?? default,
                Quantity = item.Quantity ?? default,
                UnitPrice = item.UnitPrice ?? default
            }).ToList()
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        TempData[ViewConstants.OrderSuccessMessage] = LocalizeError("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _orderModelFactory.PrepareOrderDetailsModel(id);
        if (model is null)
        {
            TempData[ViewConstants.OrderErrorMessage] = LocalizeError("Error.OrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> LockOrder(LockOrderModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery
        {
            Id = model.OrderId
        });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanLockOrder)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotLock") });

        var result = await _mediator.Send(new LockOrderCommand(model.OrderId, model.Reason!));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> AddOrderItem(AddOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateOrderItems") });

        var product = await _mediator.Send(new GetProductByIdQuery { Id = model.ProductId });
        if (product is null)
            return Json(new { success = false, message = LocalizeError("Error.ProductIsNotFound") });

        var result = await _mediator.Send(new AddOrderItemCommand
        {
            OrderId = model.OrderId,
            ProductId = model.ProductId,
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderItem(EditOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        var orderItem = order.Items.FirstOrDefault(orderItem => orderItem.Id == model.ItemId);
        if (orderItem is null)
            return Json(new { success = false, message = LocalizeError("Error.ProductIsNotFound") });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateOrderItems") });

        var result = await _mediator.Send(new UpdateOrderItemCommand
        {
            OrderId = model.OrderId,
            ItemId = model.ItemId,
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice
        });
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveOrderItem(DeleteOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        var orderItem = order.Items.FirstOrDefault(orderItem => orderItem.Id == model.ItemId);
        if (orderItem is null)
            return Json(new { success = false, message = LocalizeError("Error.ProductIsNotFound") });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateOrderItems") });

        var result = await _mediator.Send(new DeleteOrderItemCommand
        {
            OrderId = model.OrderId,
            ItemId = model.ItemId
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderNote(EditOrderNoteModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery
        {
            Id = model.OrderId
        });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanUpdateInfo)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateInfo") });

        var result = await _mediator.Send(new UpdateOrderNoteCommand(model.OrderId, model.Note!));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }
    [HttpPost]
    public async Task<IActionResult> UpdateOrderDiscount(EditOrderDiscountModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery
        {
            Id = model.OrderId
        });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanUpdateInfo)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateInfo") });

        if ((model.OrderDiscount ?? 0) > order.OrderSubTotal)
            return Json(new { success = false, message = LocalizeError("Error.OrderDiscountExceedsTotal") });

        var result = await _mediator.Send(new UpdateOrderDiscountCommand(model.OrderId, model.OrderDiscount));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateShipping(EditOrderShippingModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery
        {
            Id = model.OrderId
        });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanUpdateInfo)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateInfo") });

        var result = await _mediator.Send(new UpdateOrderShippingCommand(model.OrderId, model.ExpectedShippingDate, model.Address));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteOrderCommand(id));
        if (!resultDto.Success)
            TempData[ViewConstants.OrderErrorMessage] = LocalizeError(resultDto.ErrorMessage!);
        else
            TempData[ViewConstants.OrderSuccessMessage] = LocalizeError("Msg.DeleteSuccess");

        return RedirectToAction(nameof(List));
    }
}
