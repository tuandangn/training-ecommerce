namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

/// <summary>
/// Thông tin giá nhập gần nhất của một hàng hóa theo từng nhà cung cấp.
/// </summary>
[Serializable]
public sealed record RecentPurchasePriceDto(
    Guid? VendorId,
    string? VendorName,
    decimal UnitCost,
    string PurchaseOrderCode,
    DateTime PurchaseDateUtc);
