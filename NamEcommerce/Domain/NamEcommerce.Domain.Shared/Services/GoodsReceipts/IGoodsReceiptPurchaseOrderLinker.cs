using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

namespace NamEcommerce.Domain.Shared.Services.GoodsReceipts;

/// <summary>
/// Tách logic match-and-split GoodsReceipt items vào PurchaseOrder items khỏi GoodsReceiptManager.
/// Trách nhiệm: với GR đã tạo (có thể chưa có UnitCost), match từng (ProductId, UnitCost) với các
/// dòng PO còn lại; khi 1 item GR no-cost cần ráp với nhiều cost khác nhau từ PO thì <strong>tách
/// (split)</strong> thành nhiều item con và mỗi cái được set cost tương ứng. Mọi split đều raise
/// event <c>GoodsReceiptItemSplitOnLinking</c> để audit.
/// </summary>
public interface IGoodsReceiptPurchaseOrderLinker
{
    Task LinkAsync(SetGoodsReceiptToPurchaseOrderDto dto);
}
