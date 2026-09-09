using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Controllers;

public sealed class PaymentController(IMediator mediator) : BaseAuthorizedController
{    
    [HttpPost]
    public async Task<IActionResult> CreatePaymentIntent([FromBody] CreateBankTransferPaymentIntentCommand command)
    {
        var result = await mediator.Send(command);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.BankTransferIntentCreateFailed") });

        return Json(new { success = true, intent = result.Intent });
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentIntentStatus(Guid intentId)
    {
        var result = await mediator.Send(new GetBankTransferPaymentIntentStatusCommand { IntentId = intentId });
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
        var result = await mediator.Send(command);
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
    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsRecordPayment)]
    public async Task<IActionResult> RecordSettlementPayment([FromBody] RecordOrderSettlementPaymentCommand command)
    {
        var result = await mediator.Send(command);
        return result.Success
            ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.InvalidRequest") });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsRecordPayment)]
    public async Task<IActionResult> RecordSettlementQrPayment([FromBody] RecordOrderSettlementQrPaymentCommand command)
    {
        var result = await mediator.Send(command);
        return result.Success
            ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage ?? "Error.InvalidRequest") });
    }
}
