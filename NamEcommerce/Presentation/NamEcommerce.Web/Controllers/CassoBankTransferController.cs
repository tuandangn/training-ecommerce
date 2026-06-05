using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;

namespace NamEcommerce.Web.Controllers;

[Route("api/casso")]
public sealed class CassoBankTransferController(
    IMediator mediator,
    BankTransferPaymentSettings settings) : Controller
{
    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook()
    {
        if (!settings.Casso.Enabled || !string.Equals(settings.Verification.Provider, "Casso", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        if (!settings.Casso.WebhookEnabled)
            return NotFound();

        if (string.IsNullOrWhiteSpace(settings.Casso.WebhookSecurityKey)
            || string.IsNullOrWhiteSpace(settings.Casso.WebhookSecurityHeaderName))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var token = Request.Headers[settings.Casso.WebhookSecurityHeaderName].ToString();
        if (!string.Equals(token, settings.Casso.WebhookSecurityKey, StringComparison.Ordinal))
            return Unauthorized();

        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
            rawPayload = await reader.ReadToEndAsync().ConfigureAwait(false);

        ProcessCassoWebhookCommand? command;
        try
        {
            command = JsonSerializer.Deserialize<ProcessCassoWebhookCommand>(rawPayload, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return BadRequest(new { success = false, message = "Error.CassoWebhookPayloadMalformed" });
        }

        if (command is null)
            return BadRequest(new { success = false, message = "Error.CassoWebhookPayloadMalformed" });

        command = command with { RawPayload = rawPayload };
        var result = await mediator.Send(command).ConfigureAwait(false);

        return Ok(new
        {
            success = true,
            result.TotalRecords,
            result.Processed,
            result.Matched,
            result.Duplicate,
            result.Rejected,
            result.Ignored,
            result.Failed,
            result.Results
        });
    }

    [Authorize]
    [HttpPost("reconciliation/run")]
    public async Task<IActionResult> RunReconciliation([FromBody] RunCassoReconciliationCommand command)
    {
        var result = await mediator.Send(command).ConfigureAwait(false);
        return result.Success
            ? Ok(result)
            : BadRequest(result);
    }
}
