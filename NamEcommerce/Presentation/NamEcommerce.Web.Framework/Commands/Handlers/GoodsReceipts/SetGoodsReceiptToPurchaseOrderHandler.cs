using MediatR;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.GoodsReceipts;

public sealed class SetGoodsReceiptToPurchaseOrderHandler : IRequestHandler<SetGoodsReceiptToPurchaseOrderCommand, CommonActionResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public SetGoodsReceiptToPurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<CommonActionResultModel> Handle(SetGoodsReceiptToPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.SetGoodsReceiptToPurchaseOrderAsync(new SetGoodsReceiptToPurchaseOrderAppDto(request.Id, request.PurchaseOrderId)).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
