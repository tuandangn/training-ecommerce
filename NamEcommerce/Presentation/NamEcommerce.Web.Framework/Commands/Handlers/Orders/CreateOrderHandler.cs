using MediatR;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, CreateOrderResultModel>
{
    private readonly IOrderAppService _orderAppService;
    private readonly ICurrentUserService _currentUserService;

    public CreateOrderHandler(IOrderAppService orderAppService, ICurrentUserService currentUserService)
    {
        _orderAppService = orderAppService;
        _currentUserService = currentUserService;
    }

    public async Task<CreateOrderResultModel> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var dto = new CreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            OrderDiscount = request.OrderDiscount,
            Note = request.Note,
            ExpectedShippingDateUtc = DateTimeHelper.ToUniversalTime(request.ExpectedShippingDate),
            CreatedByUserId = currentUser?.Id
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
