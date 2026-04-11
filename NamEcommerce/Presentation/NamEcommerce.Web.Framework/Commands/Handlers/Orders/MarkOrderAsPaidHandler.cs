using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class MarkOrderAsPaidHandler : IRequestHandler<MarkOrderAsPaidCommand, CommonResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public MarkOrderAsPaidHandler(IOrderAppService orderAppService) 
        => _orderAppService = orderAppService;

    public async Task<CommonResultModel> Handle(MarkOrderAsPaidCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.MarkAsPaidAsync(new MarkOrderAsPaidAppDto
        {
            OrderId = request.OrderId,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note
        }).ConfigureAwait(false);

        return new CommonResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
