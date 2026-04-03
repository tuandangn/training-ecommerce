using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public UpdateOrderHandler(IOrderAppService orderAppService)
    {
        _orderAppService = orderAppService;
    }

    public async Task<CommonResultModel> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.UpdateOrderAsync(new UpdateOrderAppDto(request.Id)
        {
            Note = request.Note,
            OrderDiscount = request.OrderDiscount
        }).ConfigureAwait(false);

        return new CommonResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
