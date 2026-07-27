using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.FastSales;
using NamEcommerce.Web.Contracts.Models.FastSales;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Models.FastSales;

namespace NamEcommerce.Web.Controllers;

public sealed partial class OrderController : BaseAuthorizedController
{
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> QuickCreate()
    {
        var model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> QuickCreate(OrderQuickCreateModel model, OrderQuickCreatePaymentModel paymentInfo)
    {
        if (!ModelState.IsValid)
        {
            model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
            return View(model);
        }

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = model.CustomerId!.Value });
        if (customer is null)
        {
            ModelState.AddModelError(nameof(model.CustomerId), Localizer["Error.CustomerIsNotFound"]);
            model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
            return View(model);
        }

        var productIds = model.Items.Select(item => item.ProductId).OfType<Guid>().Distinct().ToList();
        var products = await _productAppService.GetProductsByIdsAsync(productIds);
        if (products.Count() != productIds.Count)
        {
            ModelState.AddModelError(string.Empty, Localizer["Error.ProductIsNotFound"]);
            model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
            return View(model);
        }

        if (model.DeliveryNow)
        {
            var allHaveWarehouses = true;
            for (var i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.WarehouseId.HasValue)
                {
                    var warehouse = _mediator.Send(new GetWarehouseByIdQuery { Id = item.WarehouseId.Value });
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
            {
                model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                return View(model);
            }

            foreach (var productGroup in model.Items.GroupBy(item => item.ProductId).ToList())
            {
                var stockInfo = await _mediator.Send(new GetProductStockInfoQuery(productGroup.Key!.Value, null));
                var totalQuantity = productGroup.Sum(item => item.Quantity);
                if (totalQuantity > stockInfo.QuantityOnHand)
                {
                    ModelState.AddModelError(string.Empty, Localizer["Error.ProductInsufficientStock"]);
                    model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                    return View(model);
                }
                foreach (var warehouseGroup in productGroup.GroupBy(item => item.WarehouseId).ToList())
                {
                    var warehouseStock = stockInfo.Warehouses.FirstOrDefault(stock => stock.WarehouseId == warehouseGroup.Key);
                    var warehouseQuantity = warehouseGroup.Sum(item => item.Quantity);
                    if (warehouseStock is null || warehouseQuantity > warehouseStock.QuantityOnHand)
                    {
                        ModelState.AddModelError(string.Empty, Localizer["Error.ProductInsufficientStock"]);
                        model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                        return View(model);
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
                    var stockInfo = await _mediator.Send(new GetProductStockInfoQuery(product.Id, null));
                    var totalQuantity = model.Items.Where(item => item.ProductId == product.Id).Sum(item => item.Quantity);
                    if (totalQuantity > stockInfo.QuantityAvailable)
                    {
                        ModelState.AddModelError(string.Empty, Localizer["Error.ProductInsufficientStock"]);
                        model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                        return View(model);
                    }
                }
            }
        }

        var orderSubTotal = model.Items.Sum(item => item.ItemSubTotal);
        var orderTotal = orderSubTotal - (paymentInfo.OrderDiscount ?? 0);
        if (orderTotal < 0)
        {
            ModelState.AddModelError(string.Empty, Localizer["Error.OrderDiscountExceedsTotal"]);
            model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
            return View(model);
        }

        if (model.PayNow)
        {
            if (paymentInfo.PaidAmount > orderTotal)
            {
                ModelState.AddModelError(string.Empty, Localizer["Error.PaidAmountNotExceededOrderTotal"]);
                model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                return View(model);
            }
            if (paymentInfo.PaymentIntentId.HasValue)
            {
                if (!_bankTransferPaymentSettings.Enabled)
                {
                    ModelState.AddModelError(string.Empty, Localizer["Error.BankTransferNotAllowed"]);
                    model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                    return View(model);
                }
                var paymentIntent = await _paymentIntentAppService.GetByIdAsync(paymentInfo.PaymentIntentId.Value);
                if (paymentIntent is null)
                {
                    ModelState.AddModelError(string.Empty, Localizer["Error.PaymentIntentIsNotFound"]);
                    model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                    return View(model);
                }
                if (paymentIntent.Amount != paymentInfo.PaidAmount)
                {
                    ModelState.AddModelError(string.Empty, Localizer["Error.PaidAmountMismatchPaymentIntentAmount"]);
                    model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                    return View(model);
                }
                if (paymentIntent.Status == (int)BankTransferPaymentIntentStatus.ManuallyConfirmed && !_bankTransferPaymentSettings.Verification.AllowManualConfirm)
                {
                    ModelState.AddModelError(string.Empty, Localizer["Error.ManuallyConfirmPaymentNotAllowed"]);
                    model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                    return View(model);
                }
            }
        }
        else if (paymentInfo.PaidAmount != 0)
        {
            ModelState.AddModelError(string.Empty, Localizer["Error.PaymentAmountMustBeZeroWhenUnpaid"]);
            model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
            return View(model);
        }


        if (model.CustomerId == _cachedValuesService.DefaultCustomerId)
        {
            if (!model.PayNow || paymentInfo.PaidAmount == 0 || (model.DeliveryNow && paymentInfo.PaidAmount != orderTotal))
            {
                ModelState.AddModelError(string.Empty, Localizer["Error.RetailWalkInCustomerMustPrepay"]);
                model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
                return View(model);
            }
        }

        var result = await _mediator.Send(new QuickCreateOrderCommand
        {
            CustomerId = model.CustomerId!.Value,
            DeliveryNow = model.DeliveryNow,
            PayNow = model.PayNow,
            Note = model.Note,
            OrderDiscount = paymentInfo.OrderDiscount,
            ShippingAddress = model.ShippingAddress ?? customer.Address,
            ShippingPhoneNumber = model.ShippingPhoneNumber ?? customer.PhoneNumber,
            PaidAmount = paymentInfo.PaidAmount,
            PaymentIntentId = paymentInfo.PaymentIntentId,
            Items = model.Items.Select(item => new QuickCreateOrderCommand.QuickCreateOrderItemModel
            {
                ProductId = item.ProductId!.Value,
                Quantity = item.Quantity ?? 0,
                UnitPrice = item.UnitPrice ?? 0,
                WarehouseId = item.WarehouseId
            }).ToList(),
        });
        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.OrderId });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> CreateCashSale([FromBody] CreateCashQuickSaleCommand command)
    {
        var result = await _mediator.Send(command);
        return ToQuickSaleJson(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> CreateBankTransferSale([FromBody] CreateBankTransferQuickSaleCommand command)
    {
        var result = await _mediator.Send(command);
        return ToQuickSaleJson(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> CreateUnpaidSale([FromBody] CreateUnpaidQuickSaleCommand command)
    {
        var result = await _mediator.Send(command);
        return ToQuickSaleJson(result);
    }


    [HttpPost]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreateBankTransferPaymentIntentCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.BankTransferIntentCreateFailed") });

        return Json(new { success = true, intent = result.Intent });
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentIntentStatus(Guid intentId)
    {
        var result = await _mediator.Send(new GetBankTransferPaymentIntentStatusCommand { IntentId = intentId });
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.PaymentIntentIsNotFound") });

        return Json(new
        {
            success = true,
            intent = result.Intent,
            status = ((BankTransferPaymentIntentStatus)result.Intent!.Status).GetDisplayText(),
            expiresAt = DateTimeHelper.ToLocalTime(result.Intent!.ExpiresAtUtc).ToString(ViewConstants.DefaultDateTimeFormat)
        });
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmPaymentIntent([FromBody] ManualConfirmBankTransferPaymentIntentCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.BankTransferIntentConfirmFailed") });

        return Json(new
        {
            success = true,
            intent = result.Intent,
            status = ((BankTransferPaymentIntentStatus)result.Intent!.Status).GetDisplayText(),
            expiresAt = DateTimeHelper.ToLocalTime(result.Intent!.ExpiresAtUtc).ToString(ViewConstants.DefaultDateTimeFormat)
        });
    }

    private IActionResult ToQuickSaleJson(QuickSaleResultModel result)
    {
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.FastSaleFailed") });

        return Json(new
        {
            success = true,
            orderId = result.OrderId,
            deliveryNoteId = result.DeliveryNoteId,
            customerDebtId = result.CustomerDebtId,
            customerPaymentId = result.CustomerPaymentId,
            orderUrl = result.OrderId.HasValue ? Url.Action("Details", "Order", new { id = result.OrderId.Value }) : null,
            orderItems = result.OrderItems.Select(item => new
            {
                orderItemId = item.OrderItemId,
                productId = item.ProductId,
                productName = item.ProductName,
                quantity = item.Quantity
            })
        });
    }
}
