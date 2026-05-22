using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Customer.Contracts.Commands;
using NamEcommerce.Customer.Contracts.Queries;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await mediator.Send(new GetCustomerOrdersQuery()).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var model = await mediator.Send(new GetCustomerOrderDetailsQuery(id)).ConfigureAwait(false);
        return model is null ? NotFound() : Ok(model);
    }
}

[ApiController]
[Route("api/order-requests")]
public sealed class OrderRequestsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await mediator.Send(new GetCustomerOrderRequestsQuery()).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var model = await mediator.Send(new GetCustomerOrderRequestDetailsQuery(id)).ConfigureAwait(false);
        return model is null ? NotFound() : Ok(model);
    }

    [HttpGet("defaults")]
    public async Task<IActionResult> GetDefaults()
        => Ok(await mediator.Send(new GetCustomerOrderRequestDefaultsQuery()).ConfigureAwait(false));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerOrderRequestCommand command)
        => Ok(await mediator.Send(command).ConfigureAwait(false));

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
        => Ok(await mediator.Send(new ConfirmCustomerOrderRequestCommand(id)).ConfigureAwait(false));
}
