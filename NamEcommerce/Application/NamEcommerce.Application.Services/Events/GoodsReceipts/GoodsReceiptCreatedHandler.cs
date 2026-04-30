using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Events.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

/// <summary>
/// Handler cho <see cref="GoodsReceiptCreated"/>. Sau khi phiếu nhập được tạo, làm 2 việc:
/// <list type="number">
///   <item><description><b>Cộng tồn kho:</b> với mỗi item có <c>WarehouseId</c>, gọi
///     <see cref="IInventoryStockManager.ReceiveStockAsync"/> để cộng số lượng. Mỗi lần cộng tồn ghi 1
///     <see cref="StockMovementLog"/> với <c>(ReferenceType = GoodsReceipt, ReferenceId = goodsReceipt.Id)</c>
///     — log này dùng làm khóa chặn việc xóa phiếu.</description></item>
///   <item><description><b>Sinh công nợ NCC (edge case):</b> nếu phiếu được tạo với đầy đủ
///     <c>VendorId</c> và tất cả items đã có <c>UnitCost</c> ngay từ đầu, sinh
///     <see cref="Domain.Entities.Debts.VendorDebt"/> tự động. Thông thường flow là tạo phiếu trước
///     rồi định giá sau (xử lý ở <see cref="GoodsReceiptItemUnitCostSetHandler"/>), nhưng nếu UI
///     cho phép set UnitCost lúc tạo thì cần xử lý ở đây.</description></item>
/// </list>
/// Idempotency của debt được đảm bảo bởi
/// <see cref="IVendorDebtManager.CreateDebtFromGoodsReceiptAsync"/> (check existing GoodsReceiptId).
/// </summary>
public sealed class GoodsReceiptCreatedHandler : INotificationHandler<GoodsReceiptCreated>
{
    private readonly IInventoryStockManager _inventoryStockManager;
    private readonly IVendorDebtManager _vendorDebtManager;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;

    public GoodsReceiptCreatedHandler(
        IInventoryStockManager inventoryStockManager,
        IVendorDebtManager vendorDebtManager,
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader)
    {
        _inventoryStockManager = inventoryStockManager;
        _vendorDebtManager = vendorDebtManager;
        _goodsReceiptDataReader = goodsReceiptDataReader;
    }

    public async Task Handle(GoodsReceiptCreated notification, CancellationToken cancellationToken)
    {
        // Re-fetch entity từ DB — event chỉ mang theo Id để tránh truyền entity reference qua event boundary.
        // Tại thời điểm handler chạy, SaveChanges đã commit nên entity chắc chắn tồn tại.
        var goodsReceipt = await _goodsReceiptDataReader.GetByIdAsync(notification.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null) return;

        // 1. Cộng tồn kho cho các item có warehouse
        foreach (var item in goodsReceipt.Items)
        {
            // Item không có warehouse (chế độ AllowNonWarehouse) → không cộng tồn.
            if (!item.WarehouseId.HasValue) continue;
            if (item.Quantity <= 0) continue;

            await _inventoryStockManager.ReceiveStockAsync(
                productId: item.ProductId,
                warehouseId: item.WarehouseId.Value,
                receivedQuantity: item.Quantity,
                note: $"Nhập từ phiếu {goodsReceipt.Id}",
                receivedByUserId: goodsReceipt.CreatedByUserId,
                referenceType: (int)StockReferenceType.GoodsReceipt,
                referenceId: goodsReceipt.Id
            ).ConfigureAwait(false);
        }

        // 2. Sinh công nợ NCC nếu phiếu được tạo với đủ điều kiện ngay từ đầu
        await TryCreateVendorDebtAsync(goodsReceipt).ConfigureAwait(false);
    }

    private async Task TryCreateVendorDebtAsync(GoodsReceipt goodsReceipt)
    {
        // Còn item chưa có giá → tổng tiền chưa xác định.
        if (goodsReceipt.IsPendingCosting()) return;
        // Chưa gắn NCC → không có chủ thể để ghi nợ.
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
