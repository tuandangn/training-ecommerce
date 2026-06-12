using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

public sealed class GoodsReceiptItemUnitCostSetHandler : INotificationHandler<GoodsReceiptItemUnitCostSet>
{
    private readonly IInventoryCostingManager _inventoryCostingManager;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;
    private readonly IVendorDebtManager _vendorDebtManager;

    public GoodsReceiptItemUnitCostSetHandler(
        IInventoryCostingManager inventoryCostingManager,
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
        IVendorDebtManager vendorDebtManager)
    {
        _inventoryCostingManager = inventoryCostingManager;
        _goodsReceiptDataReader = goodsReceiptDataReader;
        _vendorDebtManager = vendorDebtManager;
    }

    public async Task Handle(GoodsReceiptItemUnitCostSet notification, CancellationToken cancellationToken)
    {
        if (notification is null)
            return;

        var goodsReceipt = await _goodsReceiptDataReader.GetByIdAsync(notification.GoodsReceiptId, default).ConfigureAwait(false);
        if (goodsReceipt is null) return;

        await AssignInventoryCostAsync(goodsReceipt, notification.GoodsReceiptItemId).ConfigureAwait(false);
        await TryCreateVendorDebtAsync(goodsReceipt).ConfigureAwait(false);
    }

    private async Task AssignInventoryCostAsync(GoodsReceipt goodsReceipt, Guid itemId)
    {
        var item = goodsReceipt.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;
        if (!item.WarehouseId.HasValue) return;
        if (!item.UnitCost.HasValue) return;

        await _inventoryCostingManager.AssignGoodsReceiptItemCostAsync(new AssignGoodsReceiptItemCostDto
        {
            GoodsReceiptId = goodsReceipt.Id,
            GoodsReceiptItemId = item.Id,
            ProductId = item.ProductId,
            WarehouseId = item.WarehouseId.Value,
            UnitCost = item.UnitCost.Value
        }).ConfigureAwait(false);
    }

    private async Task TryCreateVendorDebtAsync(GoodsReceipt goodsReceipt)
    {
        if (goodsReceipt.IsPendingCosting()) return;
        if (!goodsReceipt.VendorId.HasValue) return;

        var totalAmount = goodsReceipt.Items.Sum(i => i.Quantity * i.UnitCost!.Value);
        if (totalAmount <= 0) return;

        await _vendorDebtManager.CreateDebtFromGoodsReceiptAsync(new CreateVendorDebtFromGoodsReceiptDto
        {
            VendorId = goodsReceipt.VendorId.Value,
            GoodsReceiptId = goodsReceipt.Id,
            TotalAmount = totalAmount
        }).ConfigureAwait(false);
    }
}
