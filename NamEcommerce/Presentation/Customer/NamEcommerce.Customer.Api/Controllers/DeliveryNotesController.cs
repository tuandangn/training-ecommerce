using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Customer.Contracts.Commands;
using NamEcommerce.Customer.Contracts.Queries;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/delivery-notes")]
public sealed class DeliveryNotesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await mediator.Send(new GetCustomerDeliveryNotesQuery()).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var model = await mediator.Send(new GetCustomerDeliveryNoteDetailsQuery(id)).ConfigureAwait(false);
        return model is null ? NotFound() : Ok(model);
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, ConfirmDeliveryNoteRequest request)
    {
        var result = await mediator.Send(new ConfirmCustomerDeliveryNoteCommand(id, request.ReceiverName, request.Note)).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:guid}/feedback")]
    public async Task<IActionResult> Feedback(Guid id, CreateFeedbackRequest request)
    {
        var result = await mediator.Send(new CreateCustomerDeliveryFeedbackCommand(id, request.Rating, request.Message)).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    public sealed record ConfirmDeliveryNoteRequest(string? ReceiverName, string? Note);
    public sealed record CreateFeedbackRequest(int? Rating, string? Message);
}
