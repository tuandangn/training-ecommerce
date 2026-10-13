using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Commands.Models.Finance;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.FastSales;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Models.Orders;
using NamEcommerce.Web.Services.Orders;

namespace NamEcommerce.Web.Controllers;

public sealed class OrderController(IMediator mediator, IOrderModelFactory orderModelFactory,
    IProductAppService productAppService, AppConfig appConfig,
    IBankTransferPaymentIntentAppService paymentIntentAppService,
    BankTransferPaymentSettings bankTransferPaymentSettings, IOrderAppService orderAppService) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.Orders.View)]
    public async Task<IActionResult> List(OrderListSearchModel search)
    {
        var model = await orderModelFactory.PrepareOrderListModel(search);

        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Orders.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await orderModelFactory.PrepareCreateOrderModel();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Create)]
    public async Task<IActionResult> Create(CreateOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        if ((model.OrderDiscount ?? 0) > model.OrderSubTotal)
        {
            ModelState.AddModelError(nameof(model.OrderDiscount), LocalizeError("Error.OrderDiscountExceedsTotal"));
            model = await orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        var result = await mediator.Send(new CreateOrderCommand
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
            model = await orderModelFactory.PrepareCreateOrderModel(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> QuickCreate()
    {
        var model = await orderModelFactory.PrepareOrderQuickCreateModelAsync();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> QuickCreate(OrderQuickCreateModel model, bool deliveryNow = false, bool returnJson = false)
    {
        if (!ModelState.IsValid)
            return await ErrorResult();

        if (!deliveryNow && model.ExpectedShippingDate.HasValue && model.ExpectedShippingDate.Value < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.ExpectedShippingDate), Localizer["Error.Invalid", Localizer["Label.ExpectedDate"]]);
            return await ErrorResult();
        }

        var customer = await mediator.Send(new GetCustomerByIdQuery { Id = model.CustomerId!.Value });
        if (customer is null)
        {
            ModelState.AddModelError(nameof(model.CustomerId), Localizer["Error.CustomerIsNotFound"]);
            return await ErrorResult();
        }

        var productIds = model.Items.Select(item => item.ProductId).OfType<Guid>().Distinct().ToList();
        var products = await productAppService.GetProductsByIdsAsync(productIds);
        if (products.Count() != productIds.Count)
        {
            ModelState.AddModelError(string.Empty, Localizer["Error.ProductIsNotFound"]);
            return await ErrorResult();
        }

        if (deliveryNow)
        {
            var allHaveWarehouses = true;
            for (var i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.WarehouseId.HasValue)
                {
                    var warehouse = await mediator.Send(new GetWarehouseByIdQuery { Id = item.WarehouseId.Value });
                    if (warehouse is null)
                    {
                        allHaveWarehouses = false;
                        ModelState.AddModelError(string.Empty, Localizer["Error.WarehouseIsNotFound"]);
                    }
                }
                else
                {
                    allHaveWarehouses = false;
                    ModelState.AddModelError($"Items[{i}].WarehouseId", Localizer["Error.Required", Localizer["Label.Warehouse"]]);
                }
            }
            if (!allHaveWarehouses)
                return await ErrorResult();

            foreach (var productGroup in model.Items.GroupBy(item => item.ProductId).ToList())
            {
                var stockInfo = await mediator.Send(new GetProductStockInfoQuery(productGroup.Key!.Value, null));
                var totalQuantity = productGroup.Sum(item => item.Quantity);
                if (totalQuantity > stockInfo.QuantityOnHand)
                {
                    ModelState.AddModelError(string.Empty, Localizer["Error.ProductInsufficientStock"]);
                    return await ErrorResult();
                }
                foreach (var warehouseGroup in productGroup.GroupBy(item => item.WarehouseId).ToList())
                {
                    var warehouseStock = stockInfo.Warehouses.FirstOrDefault(stock => stock.WarehouseId == warehouseGroup.Key);
                    var warehouseQuantity = warehouseGroup.Sum(item => item.Quantity);
                    if (warehouseStock is null || warehouseQuantity > warehouseStock.QuantityOnHand)
                    {
                        ModelState.AddModelError(string.Empty, Localizer["Error.ProductInsufficientStock"]);
                        return await ErrorResult();
                    }
                }
            }
        }
        else
        {
            foreach (var item in model.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);
                if (!product.Vendors.Any())
                {
                    var stockInfo = await mediator.Send(new GetProductStockInfoQuery(product.Id, null));
                    var totalQuantity = model.Items.Where(item => item.ProductId == product.Id).Sum(item => item.Quantity);
                    if (totalQuantity > stockInfo.QuantityAvailable)
                    {
                        ModelState.AddModelError(string.Empty, Localizer["Error.ProductInsufficientStock"]);
                        return await ErrorResult();
                    }
                }
            }
        }

        var orderSubTotal = model.Items.Sum(item => item.ItemSubTotal);
        var orderTotal = orderSubTotal - model.OrderDiscount;
        if (orderTotal < 0)
        {
            ModelState.AddModelError(string.Empty, Localizer["Error.OrderDiscountExceedsTotal"]);
            return await ErrorResult();
        }

        var result = await mediator.Send(new QuickCreateOrderCommand
        {
            CustomerId = model.CustomerId!.Value,
            DeliveryNow = deliveryNow,
            Note = model.Note,
            ExpectedShippingDate = deliveryNow ? null : model.ExpectedShippingDate,
            ShippingAddress = model.ShippingAddress,
            ShippingPhoneNumber = model.ShippingPhoneNumber,
            OrderDiscount = model.OrderDiscount,
            Items = model.Items.Select(item => new QuickCreateOrderCommand.QuickCreateOrderItemModel
            {
                ProductId = item.ProductId!.Value,
                Quantity = item.Quantity ?? 0,
                UnitPrice = item.UnitPrice ?? 0,
                WarehouseId = item.WarehouseId
            }).ToList()
        });
        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            return await ErrorResult();
        }

        return SuccessResult(result);

        //local method
        async ValueTask<IActionResult> ErrorResult()
        {
            if (returnJson)
                return this.JsonError(GetErrorMessage());

            ModelState.AddModelError(string.Empty, GetErrorMessage());
            model = await orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
            return View(model);
        }
        IActionResult SuccessResult(QuickCreateOrderResultModel info)
        {
            if (returnJson)
            {
                return this.JsonOk(new
                {
                    orderId = info.OrderId,
                    orderCode = info.OrderCode,
                    total = info.OrderTotal,
                    subTotal = info.OrderSubTotal,
                    discount = info.OrderDiscount
                });
            }

            NotifySuccess("Msg.SaveSuccess");
            return RedirectToAction(nameof(Details), new { id = info.OrderId });
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> CompleteQuickCreateOrderPayment([FromBody] OrderQuickCreateCompleteModel model)
    {
        if (!ModelState.IsValid)
            return this.JsonError(GetErrorMessage());

        var order = await orderAppService.GetOrderByIdAsync(model.OrderId);
        if (order is null)
            return this.JsonError(Localizer["Error.OrderIsNotFound"]);
        if (order.ProcessRequiresPayment && order.HadPaid)
            return this.JsonError(Localizer["Error.OrderHadPaid"]);

        var orderSubTotal = order.OrderSubTotal;
        var orderTotal = order.TotalAmount;

        if (model.PaidAmount > orderTotal)
            return this.JsonError(Localizer["Error.PaidAmountExceededOrderTotal"]);

        if (model.PaymentIntentId.HasValue)
        {
            var paymentIntent = await paymentIntentAppService.GetByIdAsync(model.PaymentIntentId.Value);
            if (paymentIntent is null)
                return this.JsonError(Localizer["Error.PaymentIntentIsNotFound"]);

            if (order.CustomerId != paymentIntent.CustomerId)
                return this.JsonError(Localizer["Error.CustomerIsMismatch"]);

            if (model.PaidAmount != paymentIntent.Amount)
                return this.JsonError(Localizer["Error.PaidAmountMismatch"]);

            if (paymentIntent.Amount > order.TotalAmount)
                return this.JsonError(Localizer["Error.PaidAmountExceededOrderTotal"]);

            if (!bankTransferPaymentSettings.Enabled)
                return this.JsonError(Localizer["Error.BankTransferNotAllowed"]);

            if (paymentIntent.Status == (int)BankTransferPaymentIntentStatus.ManuallyConfirmed && !bankTransferPaymentSettings.Verification.AllowManualConfirm)
                return this.JsonError(Localizer["Error.ManuallyConfirmPaymentNotAllowed"]);
        }

        var result = await mediator.Send(new CompleteQuickCreateOrderPaymentCommand
        {
            OrderId = model.OrderId,
            PaidAmount = model.PaidAmount,
            PaymentIntentId = model.PaymentIntentId,
        });

        if (result.Success)
            return this.JsonOk();
        return this.JsonError(result.ErrorMessage!);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> ProcessOrderWithoutPayment(Guid id)
    {
        var result = await mediator.Send(new ProcessOrderWithoutPaymentCommand(id));
        if (result.Success)
            return this.JsonOk();
        return this.JsonError(result.ErrorMessage!);
    }


    [Authorize(Policy = SystemPermissions.Orders.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await orderModelFactory.PrepareOrderDetailsModel(id);
        if (model is null)
        {
            NotifyError("Error.OrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    public async Task<IActionResult> AllocatedPurchaseOrders(Guid id)
    {
        var listModel = await mediator.Send(new GetAllocatedPurchaseOrdersForOrderQuery
        {
            OrderId = id
        });
        return PartialView("_AllocatedPurchaseOrdersOffcanvasBody", listModel);
    }

    public async Task<IActionResult> DeliveryNotes(Guid id)
    {
        var model = await orderModelFactory.PrepareOrderDetailsModel(id);
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

        var order = await mediator.Send(new GetOrderByIdQuery
        {
            Id = model.OrderId
        });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanCompleteOrder)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotComplete") });

        var result = await mediator.Send(new CompleteOrderCommand(model.OrderId));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.Cancel)]
    public async Task<IActionResult> CancelOrder([FromBody] CancelOrderModel model)
    {
        var order = await mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return this.JsonError(LocalizeError("Error.OrderIsNotFound"));

        var orderItemIds = order.Items.Select(i => i.Id).ToList();
        var result = await mediator.Send(new CancelOrderCommand(model.OrderId, orderItemIds, model.ReturnWarehouseId));

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

        var order = await mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateOrderItems") });

        var product = await mediator.Send(new GetProductByIdQuery { Id = model.ProductId });
        if (product is null)
            return Json(new { success = false, message = LocalizeError("Error.ProductIsNotFound") });

        var result = await mediator.Send(new AddOrderItemCommand
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

        var order = await mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        var orderItem = order.Items.FirstOrDefault(orderItem => orderItem.Id == model.ItemId);
        if (orderItem is null)
            return Json(new { success = false, message = LocalizeError("Error.ProductIsNotFound") });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateOrderItems") });

        var result = await mediator.Send(new UpdateOrderItemCommand
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

        var order = await mediator.Send(new GetOrderByIdQuery { Id = model.OrderId });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        var orderItem = order.Items.FirstOrDefault(orderItem => orderItem.Id == model.ItemId);
        if (orderItem is null)
            return Json(new { success = false, message = LocalizeError("Error.ProductIsNotFound") });

        if (!order.CanUpdateOrderItems)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateOrderItems") });

        var result = await mediator.Send(new DeleteOrderItemCommand
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

        var order = await mediator.Send(new GetOrderByIdQuery
        {
            Id = model.OrderId
        });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanUpdateInfo)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateInfo") });

        var result = await mediator.Send(new UpdateOrderNoteCommand(model.OrderId, model.Note!));

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

        var order = await mediator.Send(new GetOrderByIdQuery
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

        var result = await mediator.Send(new UpdateOrderDiscountCommand(model.OrderId, model.OrderDiscount));

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

        var order = await mediator.Send(new GetOrderByIdQuery
        {
            Id = model.OrderId
        });
        if (order is null)
            return Json(new { success = false, message = LocalizeError("Error.OrderIsNotFound") });

        if (!order.CanUpdateInfo)
            return Json(new { success = false, message = LocalizeError("Error.OrderCannotUpdateInfo") });

        var result = await mediator.Send(new UpdateOrderShippingCommand(model.OrderId, model.ExpectedShippingDate, model.Address, model.PhoneNumber));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> AddExpense(CreateOrderExpenseModel model)
    {
        if (!ModelState.IsValid)
            return this.JsonError(GetErrorMessage());

        var order = await orderAppService.GetOrderByIdAsync(model.OrderId);
        if (order is null)
            return this.JsonError(Localizer["Error.OrderIsNotFound"]);
        else if (model.IncurredDate < DateTimeHelper.ToLocalTime(order.CreatedOnUtc))
            return this.JsonError(Localizer["Error.ExpenseIncurredDateCannotBeLessThanOrderCreationDate"]);

        if (model.ExpenseType == ExpenseType.AssetDisposal)
            return this.JsonError(Localizer["Error.ExpenseTypeIsNotAllow"]);

        if (model.TaxRate.HasValue && !appConfig.TaxRates.Contains(model.TaxRate.Value))
            return this.JsonError(Localizer["Error.ExpenseTaxRateInvalid"]);

        var result = await mediator.Send(new CreateOrderExpenseCommand
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
        var resultDto = await mediator.Send(new DeleteOrderCommand(id));
        if (!resultDto.Success)
            NotifyError(resultDto.ErrorMessage!);
        else
            NotifySuccess("Msg.DeleteSuccess");

        return RedirectToAction(nameof(List));
    }
}
