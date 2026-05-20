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
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerOrderRequestCommand command)
        => Ok(await mediator.Send(command).ConfigureAwait(false));
}
