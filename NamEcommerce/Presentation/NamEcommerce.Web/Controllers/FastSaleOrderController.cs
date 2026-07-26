using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.FastSales;
using NamEcommerce.Web.Contracts.Models.FastSales;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Controllers;

public sealed partial class OrderController : BaseAuthorizedController
{
    [Authorize(Policy = SystemPermissions.Orders.QuickCreate)]
    public async Task<IActionResult> QuickCreate()
    {
        var model = await _fastSaleModelFactory.PrepareFastSaleModelAsync();
        return View(model);
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
