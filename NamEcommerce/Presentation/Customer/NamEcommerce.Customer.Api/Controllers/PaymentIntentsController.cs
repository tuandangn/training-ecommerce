using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Customer.Contracts.Commands;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/payment-intents")]
public sealed class PaymentIntentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerPaymentIntentCommand command)
    {
        var model = await mediator.Send(command).ConfigureAwait(false);
        return model is null ? BadRequest() : Ok(model);
    }

    [HttpPost("{id:guid}/mock-complete")]
    public async Task<IActionResult> Complete(Guid id, CompleteMockPaymentRequest request)
    {
        var model = await mediator.Send(new CompleteMockCustomerPaymentCommand(id, request.Success)).ConfigureAwait(false);
        return model is null ? BadRequest() : Ok(model);
    }

    public sealed record CompleteMockPaymentRequest(bool Success);
}
