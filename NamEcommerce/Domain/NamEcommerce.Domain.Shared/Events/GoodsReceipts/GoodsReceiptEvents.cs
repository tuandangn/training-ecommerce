namespace NamEcommerce.Domain.Shared.Events.GoodsReceipts;

/// <summary>
/// Phiếu nhập kho vừa được tạo. Handler subscribe event này để cộng tồn kho cho từng item
/// có WarehouseId, đồng thời thử sinh công nợ NCC nếu phiếu được tạo với đủ vendor + UnitCost
/// ngay từ đầu.
/// </summary>
public sealed record GoodsReceiptCreated(Guid GoodsReceiptId) : DomainEvent;

/// <summary>
/// Thông tin chung của phiếu nhập (note, truck info, ảnh đính kèm, vendor inline...) được cập nhật
/// qua <c>UpdateGoodsReceiptAsync</c>. Hiện không có handler nào subscribe — event chủ yếu để
/// audit/tracking trong tương lai.
/// </summary>
public sealed record GoodsReceiptUpdated(Guid GoodsReceiptId) : DomainEvent;

/// <summary>
/// Một dòng hàng vừa được set <c>UnitCost</c> (định giá sau khi nhập). Handler subscribe event này để:
/// <list type="number">
///   <item><description>Tính lại <c>InventoryStock.AverageCost</c> cho cặp <c>(ProductId, WarehouseId)</c>
///     theo Full Recalculation: <c>Σ(qty × cost) / Σ(qty)</c> trên các item đã có UnitCost.</description></item>
///   <item><description>Thử sinh công nợ NCC nếu phiếu đã đủ điều kiện (toàn bộ items đã định giá và
///     đã gắn vendor) — idempotent qua <c>VendorDebtManager.CreateDebtFromGoodsReceiptAsync</c>.</description></item>
/// </list>
/// </summary>
public sealed record GoodsReceiptItemUnitCostSet(Guid GoodsReceiptId, Guid GoodsReceiptItemId) : DomainEvent;

/// <summary>
/// Vendor của phiếu nhập vừa được gắn / đổi / bỏ qua <c>SetGoodsReceiptVendorAsync</c>. Handler
/// subscribe event này để thử sinh công nợ NCC khi đã đủ điều kiện (idempotent).
/// </summary>
public sealed record GoodsReceiptVendorChanged(Guid GoodsReceiptId) : DomainEvent;

/// <summary>
/// Phiếu nhập bị xoá (chỉ xảy ra khi chưa có stock movements bị block). Handler subscribe để hoàn
/// nguyên tồn kho (theo logic hiện tại — `AdjustStock`) và xoá các <see cref="Domain.Entities.Media.Picture"/>
/// đính kèm.
/// </summary>
public sealed record GoodsReceiptDeleted(Guid GoodsReceiptId, IReadOnlyCollection<Guid> PictureIds) : DomainEvent;

/// <summary>
/// Phiếu nhập vừa được link với một PurchaseOrder qua <c>SetGoodsReceiptToPurchaseOrder</c>.
/// </summary>
public sealed record GoodsReceiptSetToPurchaseOrder(Guid GoodsReceiptId, Guid PurchaseOrderId) : DomainEvent;

/// <summary>
/// Phiếu nhập vừa được unlink khỏi PurchaseOrder qua <c>RemoveGoodsReceiptFromPurchaseOrder</c>.
/// </summary>
public sealed record GoodsReceiptRemovedFromPurchaseOrder(Guid GoodsReceiptId, Guid PurchaseOrderId) : DomainEvent;
