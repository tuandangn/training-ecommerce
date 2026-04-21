using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class LockOrderHandler : IRequestHandler<LockOrderCommand, CommonActionResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public LockOrderHandler(IOrderAppService orderAppService) 
        => _orderAppService = orderAppService;

    public async Task<CommonActionResultModel> Handle(LockOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.LockOrderAsync(new LockOrderAppDto
        {
            OrderId = request.OrderId,
            Reason = request.Reason
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
