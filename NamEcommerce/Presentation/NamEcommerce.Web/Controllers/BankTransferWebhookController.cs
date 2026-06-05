using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Commands.Models.FastSales;

namespace NamEcommerce.Web.Controllers;

[Route("api/bank-transfer/webhook")]
public sealed class BankTransferWebhookController(
    IMediator mediator,
    BankTransferPaymentSettings settings) : Controller
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] ProcessBankTransferProviderTransactionCommand command)
    {
        if (!settings.Webhook.Enabled)
            return NotFound();

        if (string.IsNullOrWhiteSpace(settings.Webhook.SecretToken))
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var token = Request.Headers["X-NamEcommerce-Webhook-Token"].ToString();
        if (!string.Equals(token, settings.Webhook.SecretToken, StringComparison.Ordinal))
            return Unauthorized();

        var result = await mediator.Send(command).ConfigureAwait(false);
        return result.Success
            ? Ok(new { success = true, intent = result.Intent, verificationLogId = result.VerificationLogId })
            : BadRequest(new { success = false, message = result.ErrorMessage, verificationLogId = result.VerificationLogId });
    }
}
