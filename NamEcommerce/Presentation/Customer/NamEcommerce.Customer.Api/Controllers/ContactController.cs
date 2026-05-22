using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Customer.Contracts.Queries;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/contact")]
public sealed class ContactController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await mediator.Send(new GetCustomerContactQuery()).ConfigureAwait(false));
}
