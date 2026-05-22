using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NamEcommerce.Customer.Contracts.Queries;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/public/delivery-notes")]
[EnableRateLimiting("CustomerPublic")]
public sealed class PublicDeliveryNotesController(IMediator mediator) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> Get(string token)
    {
        var model = await mediator.Send(new GetPublicDeliveryNoteQuery(token)).ConfigureAwait(false);
        return model is null ? NotFound() : Ok(model);
    }
}
