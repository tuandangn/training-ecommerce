namespace NamEcommerce.Domain.Shared.Exceptions.Returns;

/// <summary>
/// Thrown khi tổng <c>AcceptedQuantity</c> của VendorReturn vượt quá số lượng đã nhập
/// cho cặp (GoodsReceiptId/PurchaseOrderId, ProductId). Đảm bảo không trả nhiều hơn đã nhận từ NCC.
/// </summary>
[Serializable]
public sealed class ExceedsReceivedQuantityException(Guid productId, decimal acceptedQuantity, decimal receivedQuantity)
    : NamEcommerceDomainException("Error.VendorReturn.ExceedsReceivedQuantity", productId, acceptedQuantity, receivedQuantity);
