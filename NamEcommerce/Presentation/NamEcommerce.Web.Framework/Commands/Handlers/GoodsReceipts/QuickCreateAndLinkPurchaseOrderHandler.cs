using MediatR;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Services;

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

        // Vendor: GR đã có vendor thì BẮT BUỘC dùng vendor của GR — không cho phép đổi.
        var effectiveVendorId = goodsReceipt.VendorId ?? request.VendorId;
        if (!effectiveVendorId.HasValue || effectiveVendorId.Value == Guid.Empty)
        {
            return new QuickCreateAndLinkPurchaseOrderResultModel
            {
                Success = false,
                ErrorMessage = "Error.GoodsReceipt.VendorRequired"
            };
        }

        // Định giá: cập nhật UnitCost cho các item đang Pending Costing trước khi tạo PO.
        foreach (var item in goodsReceipt.Items.Where(i => i.IsPendingCosting))
        {
            if (!request.ItemUnitCosts.TryGetValue(item.Id, out var providedCost) || providedCost <= 0)
            {
                return new QuickCreateAndLinkPurchaseOrderResultModel
                {
                    Success = false,
                    ErrorMessage = "Error.GoodsReceipt.Item.UnitCostRequired"
                };
            }

            var setCostResult = await _goodsReceiptAppService.SetGoodsReceiptItemUnitCostAsync(
                new SetGoodsReceiptItemUnitCostAppDto
                {
                    GoodsReceiptId = goodsReceipt.Id,
                    GoodsReceiptItemId = item.Id,
                    UnitCost = providedCost
                }).ConfigureAwait(false);

            if (!setCostResult.Success)
            {
                return new QuickCreateAndLinkPurchaseOrderResultModel
                {
                    Success = false,
                    ErrorMessage = setCostResult.ErrorMessage ?? "Error.GoodsReceipt.Item.SetUnitCostFailed"
                };
            }
        }

        var currentUser = await _currentUserService
            .GetCurrentUserInfoAsync()
            .ConfigureAwait(false);

        // Build PO items với UnitCost lấy từ chính GR item (đã có hoặc vừa được set ở trên).
        var poItems = goodsReceipt.Items
            .Select(i => new CreatePurchaseOrderItemAppDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost ?? (request.ItemUnitCosts.TryGetValue(i.Id, out var c) ? c : 0m)
            })
            .ToList();

        var createResult = await _purchaseOrderAppService.CreatePurchaseOrderAsync(new CreatePurchaseOrderAppDto
        {
            // Ngày đặt hàng = ngày nhận của phiếu nhập (bỏ field input riêng).
            PlacedOnUtc = goodsReceipt.ReceivedOnUtc,
            VendorId = effectiveVendorId.Value,
            WarehouseId = request.WarehouseId,
            Note = request.Note,
            CreatedByUserId = currentUser?.Id,
            Items = poItems,
            TaxAmount = request.TaxAmount,
            ShippingAmount = request.ShippingAmount
        }).ConfigureAwait(false);

        if (!createResult.Success || !createResult.CreatedId.HasValue)
        {
            return new QuickCreateAndLinkPurchaseOrderResultModel
            {
                Success = false,
                ErrorMessage = createResult.ErrorMessage
            };
        }

        var purchaseOrderId = createResult.CreatedId.Value;
        await _purchaseOrderAppService.ApprovePurchaseOrderAsync(purchaseOrderId).ConfigureAwait(false);

        var linkResult = await _goodsReceiptAppService
            .SetGoodsReceiptToPurchaseOrder(
                new SetGoodsReceiptToPurchaseOrderAppDto(request.GoodsReceiptId, purchaseOrderId))
            .ConfigureAwait(false);

        return new QuickCreateAndLinkPurchaseOrderResultModel
        {
            Success = linkResult.Success,
            ErrorMessage = linkResult.ErrorMessage,
            CreatedPurchaseOrderId = linkResult.Success ? purchaseOrderId : null
        };
    }
}
