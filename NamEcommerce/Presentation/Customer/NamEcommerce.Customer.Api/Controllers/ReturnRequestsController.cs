using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Customer.Contracts.Commands;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/return-requests")]
public sealed class ReturnRequestsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerReturnRequestCommand command)
        => Ok(await mediator.Send(command).ConfigureAwait(false));
}
