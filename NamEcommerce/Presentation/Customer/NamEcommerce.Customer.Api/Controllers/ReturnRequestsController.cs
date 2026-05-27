using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Customer.Contracts.Commands;
using NamEcommerce.Customer.Contracts.Queries;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/return-requests")]
public sealed class ReturnRequestsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await mediator.Send(new GetCustomerReturnRequestsQuery()).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var model = await mediator.Send(new GetCustomerReturnRequestDetailsQuery(id)).ConfigureAwait(false);
        return model is null ? NotFound() : Ok(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerReturnRequestCommand command)
    {
        try
        {
            return Ok(await mediator.Send(command).ConfigureAwait(false));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NamEcommerceDomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
        => Ok(await mediator.Send(new CancelCustomerReturnRequestCommand(id)).ConfigureAwait(false));
}
