using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

/// <summary>
/// Handler cho <see cref="GoodsReceiptItemUnitCostSet"/>. Khi 1 dòng hàng được set <c>UnitCost</c>:
/// <list type="number">
///   <item><description>Tính lại <c>InventoryStock.AverageCost</c> cho cặp <c>(ProductId, WarehouseId)</c>
///     theo Full Recalculation: <c>AverageCost = SUM(qty × cost) / SUM(qty)</c> trên các item đã có UnitCost.</description></item>
///   <item><description>Thử sinh công nợ NCC nếu phiếu đã đủ điều kiện (idempotent).</description></item>
/// </list>
///
/// <para><b>Tại sao Full Recalculation thay vì incremental?</b> Phiếu nhập có thể được tạo trước
/// khi biết giá, giá được điền vào sau ở các thời điểm khác nhau và không theo thứ tự —
/// incremental sẽ cho kết quả sai. Full Recalculation luôn đúng bất kể thứ tự.</para>
/// </summary>
public sealed class GoodsReceiptItemUnitCostSetHandler : INotificationHandler<GoodsReceiptItemUnitCostSet>
{
    private readonly IInventoryStockManager _inventoryStockManager;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;
    private readonly IVendorDebtManager _vendorDebtManager;

    public GoodsReceiptItemUnitCostSetHandler(
        IInventoryStockManager inventoryStockManager,
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
        IVendorDebtManager vendorDebtManager)
    {
        _inventoryStockManager = inventoryStockManager;
        _goodsReceiptDataReader = goodsReceiptDataReader;
        _vendorDebtManager = vendorDebtManager;
    }

    public async Task Handle(GoodsReceiptItemUnitCostSet notification, CancellationToken cancellationToken)
    {
        var goodsReceipt = await _goodsReceiptDataReader.GetByIdAsync(notification.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null) return;

        await RecalculateAverageCostAsync(goodsReceipt, notification.GoodsReceiptItemId).ConfigureAwait(false);
        await TryCreateVendorDebtAsync(goodsReceipt).ConfigureAwait(false);
    }

    /// <summary>
    /// Full Recalculation AverageCost cho cặp <c>(ProductId, WarehouseId)</c> của item vừa set giá.
    /// Defensive: bỏ qua nếu item không tồn tại, không có warehouse, hoặc chưa có UnitCost.
    /// </summary>
    private async Task RecalculateAverageCostAsync(GoodsReceipt goodsReceipt, Guid itemId)
    {
        var item = goodsReceipt.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null) return;

        // Item không có warehouse (chế độ AllowNonWarehouse) → không có InventoryStock để cập nhật.
        if (!item.WarehouseId.HasValue) return;
        // UnitCost chưa được set → không tính (defensive: thực tế SetUnitCost luôn set)
        if (!item.UnitCost.HasValue) return;

        var productId = item.ProductId;
        var warehouseId = item.WarehouseId.Value;

        // Full Recalculation: lấy tất cả GoodsReceiptItem cùng (ProductId, WarehouseId) đã có UnitCost
        // trên TOÀN BỘ phiếu nhập trong DB (không chỉ phiếu hiện tại).
        var pricedItems = _goodsReceiptDataReader.DataSource
            .SelectMany(gr => gr.Items)
            .Where(i => i.ProductId == productId
                     && i.WarehouseId == warehouseId
                     && i.UnitCost.HasValue)
            .Select(i => new { i.Quantity, UnitCost = i.UnitCost!.Value })
            .ToList();

        var totalQuantity = pricedItems.Sum(i => i.Quantity);
        if (totalQuantity <= 0) return;

        var totalCostValue = pricedItems.Sum(i => i.Quantity * i.UnitCost);
        var newAverageCost = totalCostValue / totalQuantity;

        await _inventoryStockManager
            .UpdateAverageCostAsync(productId, warehouseId, newAverageCost)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Sinh công nợ NCC khi phiếu nhập đủ điều kiện. Idempotent qua
    /// <see cref="IVendorDebtManager.CreateDebtFromGoodsReceiptAsync"/> (check existing GoodsReceiptId).
    /// </summary>
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
            TotalAmount = totalAmount,
            CreatedByUserId = goodsReceipt.CreatedByUserId
        }).ConfigureAwait(false);
    }
}
