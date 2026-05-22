namespace NamEcommerce.Domain.Shared.Exceptions.Returns;

/// <summary>
/// Thrown khi tổng <c>AcceptedQuantity</c> của CustomerReturn vượt quá số lượng đã giao
/// cho cặp (OrderId, ProductId). Đảm bảo khách không trả nhiều hơn đã nhận.
/// </summary>
[Serializable]
public sealed class ExceedsDeliveredQuantityException(Guid productId, decimal acceptedQuantity, decimal deliveredQuantity)
    : NamEcommerceDomainException("Error.CustomerReturn.ExceedsDeliveredQuantity", productId, acceptedQuantity, deliveredQuantity);
