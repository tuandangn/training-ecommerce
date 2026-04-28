using MediatR;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.GoodsReceipts;

/// <summary>
/// Tạo nhanh một PurchaseOrder từ items của GoodsReceipt rồi link luôn.
/// </summary>
public sealed class QuickCreateAndLinkPurchaseOrderHandler
    : IRequestHandler<QuickCreateAndLinkPurchaseOrderCommand, QuickCreateAndLinkPurchaseOrderResultModel>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly ICurrentUserService _currentUserService;
    
    public QuickCreateAndLinkPurchaseOrderHandler(
        IGoodsReceiptAppService goodsReceiptAppService,
        IPurchaseOrderAppService purchaseOrderAppService,
        ICurrentUserService currentUserService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
        _purchaseOrderAppService = purchaseOrderAppService;
        _currentUserService = currentUserService;
    }

    public async Task<QuickCreateAndLinkPurchaseOrderResultModel> Handle(
        QuickCreateAndLinkPurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var goodsReceipt = await _goodsReceiptAppService
            .GetGoodsReceiptByIdAsync(request.GoodsReceiptId)
            .ConfigureAwait(false);

        if (goodsReceipt is null)
        {
            return new QuickCreateAndLinkPurchaseOrderResultModel
            {
                Success = false,
                ErrorMessage = "Error.GoodsReceipt.IsNotFound"
            };
        }

        if (goodsReceipt.PurchaseOrderId.HasValue)
        {
            return new QuickCreateAndLinkPurchaseOrderResultModel
            {
                Success = false,
                ErrorMessage = "Error.GoodsReceipt.AlreadyLinkedToPurchaseOrder"
            };
        }

        var currentUser = await _currentUserService
            .GetCurrentUserInfoAsync()
            .ConfigureAwait(false);

        var poItems = goodsReceipt.Items
            .Select(i => new CreatePurchaseOrderItemAppDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost ?? 0m
            })
            .ToList();

        var createResult = await _purchaseOrderAppService.CreatePurchaseOrderAsync(new CreatePurchaseOrderAppDto
        {
            PlacedOnUtc = DateTimeHelper.ToUniversalTime(request.PlacedOn),
            VendorId = request.VendorId,
            WarehouseId = request.WarehouseId,
            Note = request.Note,
            CreatedByUserId = currentUser?.Id,
            Items = poItems
        }).ConfigureAwait(false);

        if (!createResult.Success || !createResult.CreatedId.HasValue)
        {
            return new QuickCreateAndLinkPurchaseOrderResultModel
            {
                Success = false,
                ErrorMessage = createResult.ErrorMessage
            };
        }

        var purchaseOrderId = createResult.CreatedId;
        await _purchaseOrderAppService.ApprovePurchaseOrderAsync(purchaseOrderId.Value).ConfigureAwait(false);

        var linkResult = await _goodsReceiptAppService
            .SetGoodsReceiptToPurchaseOrder(
                new SetGoodsReceiptToPurchaseOrderAppDto(request.GoodsReceiptId, createResult.CreatedId.Value))
            .ConfigureAwait(false);

        return new QuickCreateAndLinkPurchaseOrderResultModel
        {
            Success = linkResult.Success,
            ErrorMessage = linkResult.ErrorMessage,
            CreatedPurchaseOrderId = linkResult.Success ? createResult.CreatedId : null
        };
    }
}
