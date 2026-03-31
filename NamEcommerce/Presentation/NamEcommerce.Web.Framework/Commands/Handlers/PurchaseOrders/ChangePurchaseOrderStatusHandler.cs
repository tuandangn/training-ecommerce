using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ChangePurchaseOrderStatusHandler : IRequestHandler<ChangePurchaseOrderStatusCommand, ChangePurchaseOrderStatusResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public ChangePurchaseOrderStatusHandler(IPurchaseOrderAppService appService)
    {
        _purchaseOrderAppService = appService;
    }

    public async Task<ChangePurchaseOrderStatusResultModel> Handle(ChangePurchaseOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.ChangeStatusAsync(request.PurchaseOrderId, request.Status).ConfigureAwait(false);

        return new ChangePurchaseOrderStatusResultModel
        {
            Success = result.success,
            ErrorMessage = result.errorMessage
        };
    }
}
