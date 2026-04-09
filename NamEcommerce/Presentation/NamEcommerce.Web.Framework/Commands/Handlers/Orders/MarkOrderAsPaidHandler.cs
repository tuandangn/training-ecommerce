using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class MarkOrderAsPaidHandler : IRequestHandler<MarkOrderAsPaidCommand, MarkOrderAsPaidResultModel>
{
    private readonly IOrderAppService _orderAppService;

    public MarkOrderAsPaidHandler(IOrderAppService orderAppService) 
        => _orderAppService = orderAppService;

    public async Task<MarkOrderAsPaidResultModel> Handle(MarkOrderAsPaidCommand request, CancellationToken cancellationToken)
    {
        var result = await _orderAppService.MarkAsPaidAsync(new MarkOrderAsPaidAppDto
        {
            OrderId = request.OrderId,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note
        });

        return new MarkOrderAsPaidResultModel(result.Success, result.ErrorMessage);
    }
}
