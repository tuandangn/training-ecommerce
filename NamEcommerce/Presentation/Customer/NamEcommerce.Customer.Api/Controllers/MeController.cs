using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Customer.Contracts.Queries;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/me")]
public sealed class MeController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var model = await mediator.Send(new GetCurrentCustomerSessionQuery()).ConfigureAwait(false);
        return model is null ? Unauthorized() : Ok(model);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
        => Ok(await mediator.Send(new GetCustomerDashboardQuery()).ConfigureAwait(false));
}
