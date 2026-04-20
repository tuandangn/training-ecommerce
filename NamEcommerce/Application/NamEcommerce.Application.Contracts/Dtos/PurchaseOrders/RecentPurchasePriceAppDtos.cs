namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

/// <summary>
/// Giá nhập gần nhất của một hàng hóa theo nhà cung cấp, dùng để gợi ý khi tạo đơn nhập.
/// </summary>
[Serializable]
public sealed record RecentPurchasePriceAppDto(
    Guid? VendorId,
    string? VendorName,
    decimal UnitCost,
    string PurchaseOrderCode,
    DateTime PurchaseDate);
