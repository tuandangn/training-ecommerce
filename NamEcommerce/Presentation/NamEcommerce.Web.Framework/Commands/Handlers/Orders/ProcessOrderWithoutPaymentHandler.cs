using MediatR;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class ProcessOrderWithoutPaymentHandler(IQuickCreateOrderAppService quickCreateOrderAppService) : IRequestHandler<ProcessOrderWithoutPaymentCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(ProcessOrderWithoutPaymentCommand request, CancellationToken cancellationToken)
    {
        var result = await quickCreateOrderAppService.ProcessOrderWithoutPaymentAsync(request.OrderId);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
