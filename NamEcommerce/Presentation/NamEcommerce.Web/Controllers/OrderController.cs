using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
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

        if (model.Items.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn hàng hóa.");
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        var orderSubTotal = model.Items.Sum(item => item.ItemSubTotal);
        if ((model.OrderDiscount ?? 0) > orderSubTotal)
        {
            ModelState.AddModelError(nameof(model.OrderDiscount), $"Giảm giá tối đa không quá {orderSubTotal.ToString(ViewConstants.NumberCustomFormat)}");
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
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        TempData[ViewConstants.OrderSuccessMessage] = "Tạo đơn hàng thành công!";
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _orderModelFactory.PrepareOrderDetailsModel(id);
        if (model is null)
        {
            TempData[ViewConstants.OrderErrorMessage] = "Order is not found.";
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
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

        if (!order.CanLockOrder)
            return Json(new { success = false, message = "Đơn hàng không thể khóa lúc này." });

        var result = await _mediator.Send(new LockOrderCommand(model.OrderId, model.Reason!));

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> AddOrderItem(AddOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = "Không thể cập nhật hàng hóa." });

        var product = await _mediator.Send(new GetProductByIdQuery { Id = model.ProductId });
        if (product is null)
            return Json(new { success = false, message = "Không tìm thấy hàng hóa." });

        var result = await _mediator.Send(new AddOrderItemCommand
        {
            OrderId = model.OrderId,
            ProductId = model.ProductId,
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice
        });

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderItem(EditOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

        var orderItem = order.Items.FirstOrDefault(orderItem => orderItem.Id == model.ItemId);
        if (orderItem is null)
            return Json(new { success = false, message = "Không tìm thấy hàng hóa." });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = "Không thể cập nhật hàng hóa." });

        var result = await _mediator.Send(new UpdateOrderItemCommand
        {
            OrderId = model.OrderId,
            ItemId = model.ItemId,
            Quantity = model.Quantity,
            UnitPrice = model.UnitPrice
        });
        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> RemoveOrderItem(DeleteOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

        var orderItem = order.Items.FirstOrDefault(orderItem => orderItem.Id == model.ItemId);
        if (orderItem is null)
            return Json(new { success = false, message = "Không tìm thấy hàng hóa." });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = "Không thể cập nhật hàng hóa." });

        var result = await _mediator.Send(new DeleteOrderItemCommand
        {
            OrderId = model.OrderId,
            ItemId = model.ItemId
        });

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });
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
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

        if (!order.CanUpdateInfo)
            return Json(new { success = false, message = "Đơn hàng không thể thay đổi lúc này." });

        var result = await _mediator.Send(new UpdateOrderNoteCommand(model.OrderId, model.Note!));

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

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
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

        if (!order.CanUpdateInfo)
            return Json(new { success = false, message = "Đơn hàng không thể thay đổi lúc này." });

        if ((model.OrderDiscount ?? 0) > order.OrderSubTotal)
            return Json(new { success = false, message = "Giảm giá không được lớn hơn tổng đơn." });

        var result = await _mediator.Send(new UpdateOrderDiscountCommand(model.OrderId, model.OrderDiscount));

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

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
            return Json(new { success = false, message = "Không tìm thấy đơn hàng." });

        if (!order.CanUpdateInfo)
            return Json(new { success = false, message = "Đơn hàng không thể thay đổi lúc này." });

        var result = await _mediator.Send(new UpdateOrderShippingCommand(model.OrderId, model.ExpectedShippingDate, model.Address));

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, message = string.Empty });
    }
}
