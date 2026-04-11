using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class ChangeOrderStatusHandler : IRequestHandler<ChangeOrderStatusCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public ChangeOrderStatusHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CommonResultModel> Handle(ChangeOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.ChangeOrderStatusAsync(request.OrderId, request.Status).ConfigureAwait(false);

        return new CommonResultModel
        {
            Success = result.success,
            ErrorMessage = result.errorMessage
        };
    }
}
