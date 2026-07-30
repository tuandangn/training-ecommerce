using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class CompleteQuickCreateOrderPaymentHandler(IQuickCreateOrderAppService fastSaleAppService, IOrderAppService orderAppService)
    : IRequestHandler<CompleteQuickCreateOrderPaymentCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CompleteQuickCreateOrderPaymentCommand request, CancellationToken cancellationToken)
    {
        var result = await fastSaleAppService.CompleteQuickCreateOrderPaymentAsync(new CompleteQuickCreateOrderPaymentAppDto
        {
            OrderId = request.OrderId,
            PaidAmount = request.PaidAmount,
            PaymentIntentId = request.PaymentIntentId
        });

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
