using MediatR;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Application.Contracts.Orders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class ChangeOrderStatusHandler : IRequestHandler<ChangeOrderStatusCommand, ChangeOrderStatusResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public ChangeOrderStatusHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<ChangeOrderStatusResultModel> Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.ChangeOrderStatusAsync(request.OrderId, request.Status).ConfigureAwait(false);

        return new ChangeOrderStatusResultModel
        {
            Success = result.success,
            ErrorMessage = result.errorMessage
        };
    }
}
