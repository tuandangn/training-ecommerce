using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

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
        if ((PurchaseOrderStatus)request.Status == PurchaseOrderStatus.Approved)
        {
            return new ChangePurchaseOrderStatusResultModel
            {
                Success = false,
                ErrorMessage = "Error.PurchaseOrder.ApproveRequiresPermission"
            };
        }

        var (success, errorMessage) = await _purchaseOrderAppService.ChangeStatusAsync(request.PurchaseOrderId, request.Status).ConfigureAwait(false);

        return new ChangePurchaseOrderStatusResultModel
        {
            Success = success,
            ErrorMessage = errorMessage
        };
    }
}
