using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class BulkReceivePurchaseOrderHandler : IRequestHandler<BulkReceivePurchaseOrderCommand, BulkReceivePurchaseOrderResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly ICurrentUserService _currentUserService;

    public BulkReceivePurchaseOrderHandler(IPurchaseOrderAppService appService, ICurrentUserService currentUserService)
    {
        _purchaseOrderAppService = appService;
        _currentUserService = currentUserService;
    }

    public async Task<BulkReceivePurchaseOrderResultModel> Handle(BulkReceivePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);

        var dto = new BulkReceiveGoodsAppDto(request.PurchaseOrderId)
        {
            ReceivedByUserId = currentUser?.Id,
            AdditionalShipping = request.AdditionalShipping,
            AdditionalTax = request.AdditionalTax,
            Items = request.Items.Select(i => new BulkReceiveItemAppDto
            {
                ItemId = i.ItemId,
                Quantity = i.Quantity,
                WarehouseId = i.WarehouseId,
                ActualUnitCost = i.ActualUnitCost
            }).ToList()
        };

        var result = await _purchaseOrderAppService.BulkReceiveAsync(dto).ConfigureAwait(false);

        return new BulkReceivePurchaseOrderResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedGoodsReceiptIds = result.CreatedGoodsReceiptIds
        };
    }
}
