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
    public async Task<IActionResult> QuickCreate(OrderQuickCreateModel model, bool deliveryNow = false, bool returnJson = false)
    {
        if (!ModelState.IsValid)
            return await ErrorResult();

        var customer = await _mediator.Send(new GetCustomerByIdQuery { Id = model.CustomerId!.Value });
        if (customer is null)
        {
            ModelState.AddModelError(nameof(model.CustomerId), Localizer["Error.CustomerIsNotFound"]);
            return await ErrorResult();
        }

        var productIds = model.Items.Select(item => item.ProductId).OfType<Guid>().Distinct().ToList();
        var products = await _productAppService.GetProductsByIdsAsync(productIds);
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
                    var warehouse = await _mediator.Send(new GetWarehouseByIdQuery { Id = item.WarehouseId.Value });
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
                var stockInfo = await _mediator.Send(new GetProductStockInfoQuery(productGroup.Key!.Value, null));
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
                    var stockInfo = await _mediator.Send(new GetProductStockInfoQuery(product.Id, null));
                    var totalQuantity = model.Items.Where(item => item.ProductId == product.Id).Sum(item => item.Quantity);
                    if (totalQuantity > stockInfo.QuantityAvailable)
                    {
                        ModelState.AddModelError(string.Empty, Localizer["Error.ProductInsufficientStock"]);
                        return await ErrorResult();
                    }
                }
            }
        }

        var result = await _mediator.Send(new QuickCreateOrderCommand
        {
            CustomerId = model.CustomerId!.Value,
            DeliveryNow = deliveryNow,
            Note = model.Note,
            ShippingAddress = model.ShippingAddress,
            ShippingPhoneNumber = model.ShippingPhoneNumber,
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

        return SuccessResult(result.OrderId!.Value);

        //local method
        async ValueTask<IActionResult> ErrorResult()
        {
            if (returnJson)
                return this.JsonError(GetErrorMessage());

            ModelState.AddModelError(string.Empty, GetErrorMessage());
            model = await _orderModelFactory.PrepareOrderQuickCreateModelAsync(model);
            return View(model);
        }
        IActionResult SuccessResult(Guid createdOrderId)
        {
            if (returnJson)
                return this.JsonOk(new { createdOrderId });

            NotifySuccess("Msg.SaveSuccess");
            return RedirectToAction(nameof(Details), new { id = createdOrderId });
        }
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
