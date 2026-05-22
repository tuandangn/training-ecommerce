using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Customer.Contracts.Queries;

namespace NamEcommerce.Customer.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? categoryId, [FromQuery] string? keywords, [FromQuery] bool purchasedOnly = true, [FromQuery] int pageSize = 30)
        => Ok(await mediator.Send(new GetCustomerProductsQuery(categoryId, keywords, purchasedOnly, pageSize)).ConfigureAwait(false));

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
        => Ok(await mediator.Send(new GetCustomerProductCategoriesQuery()).ConfigureAwait(false));
}
