using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Services.Common;
using NamEcommerce.Web.Services.Orders;

namespace NamEcommerce.Web.Controllers;

public sealed partial class OrderController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IOrderAppService _orderAppService;
    private readonly IOrderModelFactory _orderModelFactory;
    private readonly IProductAppService _productAppService;
    private readonly ICachedValuesService _cachedValuesService;
    private readonly IBankTransferPaymentIntentAppService _paymentIntentAppService;
    private readonly BankTransferPaymentSettings _bankTransferPaymentSettings;
    private readonly AppConfig _appConfig;

    public OrderController(IMediator mediator, IOrderModelFactory orderModelFactory,
        IProductAppService productAppService, ICachedValuesService cachedValuesService,
        IBankTransferPaymentIntentAppService paymentIntentAppService,
        BankTransferPaymentSettings bankTransferPaymentSettings, IOrderAppService orderAppService,
        AppConfig appConfig)
    {
        _mediator = mediator;
        _orderModelFactory = orderModelFactory;
        _productAppService = productAppService;
        _cachedValuesService = cachedValuesService;
        _paymentIntentAppService = paymentIntentAppService;
        _bankTransferPaymentSettings = bankTransferPaymentSettings;
        _orderAppService = orderAppService;
        _appConfig = appConfig;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.Orders.View)]
    public async Task<IActionResult> List(OrderListSearchModel search)
    {
        var model = await _orderModelFactory.PrepareOrderListModel(search);

        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Orders.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await _orderModelFactory.PrepareCreateOrderModel();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Create)]
    public async Task<IActionResult> Create(CreateOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        if ((model.OrderDiscount ?? 0) > model.OrderSubTotal)
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
            ShippingPhoneNumber = model.ShippingPhoneNumber!,
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

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    [Authorize(Policy = SystemPermissions.Orders.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _orderModelFactory.PrepareOrderDetailsModel(id);
        if (model is null)
        {
            NotifyError("Error.OrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    public async Task<IActionResult> AllocatedPurchaseOrders(Guid id)
    {
        var listModel = await _mediator.Send(new GetAllocatedPurchaseOrdersForOrderQuery
        {
            OrderId = id
        });
        return PartialView("_AllocatedPurchaseOrdersOffcanvasBody", listModel);
    }

    public async Task<IActionResult> DeliveryNotes(Guid id)
    {
        var model = await _orderModelFactory.PrepareOrderDetailsModel(id);
        if (model is null)
            return NotFound();

        return PartialView("_DeliveryNotesOffcanvasBody", model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
    public async Task<IActionResult> CompleteOrder(CompleteOrderModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var order = await _mediator.Send(new GetOrderByIdQuery
        {
            Id = model.OrderId
        });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanCompleteOrder)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotComplete") });

        var result = await _mediator.Send(new CompleteOrderCommand(model.OrderId));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsRecordPayment)]
    public async Task<IActionResult> RecordSettlementPayment([FromBody] RecordOrderSettlementPaymentCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success
            ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.InvalidRequest") });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsRecordPayment)]
    public async Task<IActionResult> RecordSettlementQrPayment([FromBody] RecordOrderSettlementQrPaymentCommand command)
    {
        var result = await _mediator.Send(command);
        return result.Success
            ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.InvalidRequest") });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Cancel)]
    public async Task<IActionResult> CancelOrder([FromBody] CancelOrderModel model)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return this.JsonError(LocalizeError("Error.OrderIsNotFound"));

        var orderItemIds = order.Items.Select(i => i.Id).ToList();
        var result = await _mediator.Send(new CancelOrderCommand(model.OrderId, orderItemIds, model.ReturnWarehouseId));

        if (!result.Success)
            return this.JsonError(result.ErrorMessage!);

        return this.JsonOk(message: LocalizeError("Error.OrderCancelled"));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
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
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
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
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
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
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
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
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
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

        var discountAmount = (model.OrderDiscount ?? 0);

        if (discountAmount > order.OrderSubTotal)
            return Json(new { success = false, message = LocalizeError("Error.OrderDiscountExceedsTotal") });

        var orderTotal = order.OrderSubTotal - discountAmount;
        if (orderTotal < order.PaidAmount)
            return Json(new { success = false, message = LocalizeError("Error.AfterDiscountOrderTotalCannotBeLessThanPaidAmount") });

        var result = await _mediator.Send(new UpdateOrderDiscountCommand(model.OrderId, model.OrderDiscount));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Edit)]
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

        var result = await _mediator.Send(new UpdateOrderShippingCommand(model.OrderId, model.ExpectedShippingDate, model.Address, model.PhoneNumber));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> AddExpense(CreateOrderExpenseModel model)
    {
        if (!ModelState.IsValid)
            return this.JsonError(GetErrorMessage());

        var order = await _orderAppService.GetOrderByIdAsync(model.OrderId);
        if (order is null)
            return this.JsonError(Localizer["Error.OrderIsNotFound"]);
        else if (model.IncurredDate < DateTimeHelper.ToLocalTime(order.CreatedOnUtc))
            return this.JsonError(Localizer["Error.ExpenseIncurredDateCannotBeLessThanOrderCreationDate"]);

        if (model.ExpenseType == ExpenseType.AssetDisposal)
            return this.JsonError(Localizer["Error.ExpenseTypeIsNotAllow"]);

        if (model.TaxRate.HasValue && !_appConfig.TaxRates.Contains(model.TaxRate.Value))
            return this.JsonError(Localizer["Error.ExpenseTaxRateInvalid"]);

        var result = await _mediator.Send(new CreateOrderExpenseCommand
        {
            Title = model.Title,
            AmountWithoutTax = model.AmountWithoutTax,
            TaxRate = model.TaxRate,
            ExpenseType = (int)model.ExpenseType,
            IncurredDate = model.IncurredDate,
            Description = model.Description,
            OrderId = model.OrderId
        });
        if (!result.Success)
            return this.JsonError(result.ErrorMessage!);

        return this.JsonOk(message: Localizer["Msg.SaveSuccess"]);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteOrderCommand(id));
        if (!resultDto.Success)
            NotifyError(resultDto.ErrorMessage!);
        else
            NotifySuccess("Msg.DeleteSuccess");

        return RedirectToAction(nameof(List));
    }
}
