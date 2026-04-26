using MediatR;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Events.GoodsReceipts;

/// <summary>
/// Handler cho <see cref="EntityUpdatedNotification{T}"/> của <see cref="GoodsReceipt"/>.
/// Phân biệt loại update qua <c>AdditionalData</c>:
/// <list type="bullet">
///   <item><description><c>Guid itemId</c> — vừa set <c>UnitCost</c> cho item: tính lại
///     <c>AverageCost</c> theo Full Recalculation, sau đó thử sinh công nợ NCC nếu đủ điều kiện.</description></item>
///   <item><description><c>"vendor-updated"</c> — vừa gắn/đổi NCC: thử sinh công nợ NCC nếu đủ điều kiện
///     (không tính lại AverageCost).</description></item>
///   <item><description>Các flow update khác (note, ảnh, truck info...): bỏ qua.</description></item>
/// </list>
///
/// <para><b>AverageCost — Full Recalculation:</b>
/// <code>
/// AverageCost = SUM(qty × cost) / SUM(qty)   ← chỉ tính các item đã có UnitCost
/// </code>
/// Lý do dùng Full Recalculation thay vì incremental: phiếu nhập có thể được tạo trước
/// khi biết giá, giá được điền vào sau ở các thời điểm khác nhau và không theo thứ tự
/// → incremental sẽ cho kết quả sai. Full Recalculation luôn đúng bất kể thứ tự.</para>
///
/// <para><b>Sinh công nợ tự động:</b> Cần đồng thời (a) tất cả items đã được định giá
/// (<c>!IsPendingCosting()</c>) và (b) đã gắn NCC (<c>VendorId.HasValue</c>). Tổng tiền =
/// <c>SUM(item.Quantity × item.UnitCost)</c>. Idempotent — gọi lại nhiều lần chỉ tạo 1 phiếu nợ
/// (đảm bảo trong <see cref="IVendorDebtManager.CreateDebtFromGoodsReceiptAsync"/>).</para>
/// </summary>
public sealed class GoodsReceiptUpdatedHandler : INotificationHandler<EntityUpdatedNotification<GoodsReceipt>>
{
    private readonly IInventoryStockManager _inventoryStockManager;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;
    private readonly IVendorDebtManager _vendorDebtManager;

    private const string VendorUpdatedMarker = "vendor-updated";

    public GoodsReceiptUpdatedHandler(
        IInventoryStockManager inventoryStockManager,
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
        IVendorDebtManager vendorDebtManager)
    {
        _inventoryStockManager = inventoryStockManager;
        _goodsReceiptDataReader = goodsReceiptDataReader;
        _vendorDebtManager = vendorDebtManager;
    }

    public async Task Handle(EntityUpdatedNotification<GoodsReceipt> notification, CancellationToken cancellationToken)
    {
        var goodsReceipt = notification.Entity;
        if (goodsReceipt is null) return;

        switch (notification.AdditionalData)
        {
            case Guid itemId:
                // Set UnitCost: tính lại AverageCost rồi thử sinh công nợ NCC.
                await RecalculateAverageCostAsync(goodsReceipt, itemId).ConfigureAwait(false);
                await TryCreateVendorDebtAsync(goodsReceipt).ConfigureAwait(false);
                break;

            case string s when s == VendorUpdatedMarker:
                // Đổi/gắn NCC: chỉ thử sinh công nợ (không cần tính lại AverageCost).
                await TryCreateVendorDebtAsync(goodsReceipt).ConfigureAwait(false);
                break;

            // Các flow update khác (note, picture, truck info, IEnumerable<Guid> deletedPictureIds...)
            // → không action gì.
            default:
                return;
        }
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
        // Defensive: lý thuyết không xảy ra vì item hiện tại chắc chắn nằm trong tập trên,
        // nhưng nếu DB filter trả về rỗng thì bỏ qua để tránh chia 0.
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
        // Còn item chưa có giá → chưa biết tổng tiền, hoãn lại đến khi định giá xong.
        if (goodsReceipt.IsPendingCosting()) return;
        // Chưa gắn NCC → không có chủ thể để ghi nợ.
        if (!goodsReceipt.VendorId.HasValue) return;

        // Tổng tiền nợ = Σ(qty × unitCost). UnitCost chắc chắn HasValue do đã pass IsPendingCosting().
        var totalAmount = goodsReceipt.Items.Sum(i => i.Quantity * i.UnitCost!.Value);
        // Defensive: phiếu rỗng hoặc tổng tiền 0 → không tạo nợ.
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
