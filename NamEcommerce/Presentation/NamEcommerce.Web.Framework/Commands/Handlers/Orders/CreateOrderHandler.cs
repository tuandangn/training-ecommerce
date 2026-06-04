using MediatR;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Contracts.Models.Orders;
using NamEcommerce.Web.Contracts.Extensions;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public CreateOrderHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CreateOrderResultModel> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.Items.Any(i => i.QuantityDecimalPlaces == 0 && i.Quantity != Math.Floor(i.Quantity)))
            return new CreateOrderResultModel { Success = false, ErrorMessage = "Error.QuantityMustBeInteger" };

        var dto = new CreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            ExpectedShippingDateUtc = DateTimeHelper.ToUniversalTime(request.ExpectedShippingDate.ToEndOfDate()),
            ShippingAddress = request.ShippingAddress
        };
        foreach (var item in request.Items)
        {
            dto.Items.Add(new CreateOrderAppDto.OrderItemAppDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        var result = await _orderAppService.CreateOrderAsync(dto).ConfigureAwait(false);

        return new CreateOrderResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
