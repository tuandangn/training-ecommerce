using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.FastSales;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;

namespace NamEcommerce.Web.Controllers;

public sealed partial class OrderController : BaseAuthorizedController
{
    public async Task<IActionResult> FastCreate()
    {
        var model = await _fastSaleModelFactory.PrepareFastSaleModelAsync().ConfigureAwait(false);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SearchProducts(string? keywords, Guid? warehouseId)
    {
        var products = await _mediator.Send(new GetProductListForOrderQuery
        {
            Keywords = keywords,
            WarehouseId = warehouseId
        }).ConfigureAwait(false);

        return Json(new
        {
            items = products.Data.Items.Select(item => new
            {
                id = item.Id,
                name = item.Name,
                unitPrice = item.UnitPrice,
                unitMeasurement = item.UnitMeasurement,
                quantityDecimalPlaces = item.QuantityDecimalPlaces,
                quantityOnHand = item.QuantityOnHand,
                quantityReserved = item.QuantityReserved,
                quantityAvailable = item.QuantityAvailable,
                pictureUrl = item.PictureUrl,
                availableWarehouses = item.AvailableWarehouses.Select(warehouse => new
                {
                    id = warehouse.Id,
                    name = warehouse.Name
                })
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreateBankTransferPaymentIntentCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.BankTransferIntentCreateFailed") });

        return Json(new { success = true, intent = result.Intent });
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentIntentStatus(Guid intentId)
    {
        var result = await _mediator.Send(new GetBankTransferPaymentIntentStatusCommand { IntentId = intentId }).ConfigureAwait(false);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.PaymentIntentIsNotFound") });

        return Json(new { success = true, intent = result.Intent });
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmPaymentIntent([FromBody] ManualConfirmBankTransferPaymentIntentCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.BankTransferIntentConfirmFailed") });

        return Json(new { success = true, intent = result.Intent });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCashSale([FromBody] CreateCashQuickSaleCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        return ToQuickSaleJson(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBankTransferSale([FromBody] CreateBankTransferQuickSaleCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        return ToQuickSaleJson(result);
    }

    private IActionResult ToQuickSaleJson(Contracts.Models.FastSales.QuickSaleResultModel result)
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
            orderUrl = result.OrderId.HasValue ? Url.Action("Details", "Order", new { id = result.OrderId.Value }) : null
        });
    }

}
