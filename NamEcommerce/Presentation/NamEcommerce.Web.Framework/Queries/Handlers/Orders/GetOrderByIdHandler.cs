using MediatR;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Web.Contracts.Models.Orders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Orders;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailsModel?>
{
    private readonly IOrderAppService _orderAppService;
    private readonly IProductAppService _productAppService;
    private readonly ICustomerAppService _customerAppService;

    public GetOrderByIdHandler(IOrderAppService orderAppService, IProductAppService productAppService, ICustomerAppService customerAppService)
    {
        _orderAppService = orderAppService;
        _productAppService = productAppService;
        _customerAppService = customerAppService;
    }

    public async Task<OrderDetailsModel?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _orderAppService.GetOrderByIdAsync(request.Id).ConfigureAwait(false);
        if (dto is null) return null;

        var customer = await _customerAppService.GetCustomerByIdAsync(dto.CustomerId).ConfigureAwait(false);

        var model = new OrderDetailsModel
        {
            Id = dto.Id,
            CustomerId = dto.CustomerId,
            CustomerName = customer?.FullName ?? "Unknown Customer",
            TotalAmount = dto.TotalAmount,
            Status = dto.Status,
            Note = dto.Note
        };
        foreach (var it in dto.Items)
        {
            var product = await _productAppService.GetProductByIdAsync(it.ProductId).ConfigureAwait(false);
            model.Items.Add(new OrderDetailsItemModel 
            { 
                ItemId = it.ItemId, 
                ProductId = it.ProductId, 
                ProductName = product?.Name ?? "Unknown Product",
                Quantity = it.Quantity, 
                UnitPrice = it.UnitPrice 
            });
        }
        return model;
    }
}
