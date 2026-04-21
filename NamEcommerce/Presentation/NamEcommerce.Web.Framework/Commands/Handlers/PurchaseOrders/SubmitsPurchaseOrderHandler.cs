using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class SubmitsPurchaseOrderHandler : IRequestHandler<SubmitsPurchaseOrderCommand, CommonActionResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public SubmitsPurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<CommonActionResultModel> Handle(SubmitsPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var (success, errorMessage) = await _purchaseOrderAppService.SubmitsPurchaseOrderAsync(request.PurchaseOrderId).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = success,
            ErrorMessage = errorMessage
        };
    }
}
