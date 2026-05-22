using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CompleteOrderHandler : IRequestHandler<CompleteOrderCommand, CommonActionResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public CompleteOrderHandler(IOrderAppService orderAppService)
        => _orderAppService = orderAppService;

    public async Task<CommonActionResultModel> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.CompleteOrderAsync(new CompleteOrderAppDto
        {
            OrderId = request.OrderId
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
