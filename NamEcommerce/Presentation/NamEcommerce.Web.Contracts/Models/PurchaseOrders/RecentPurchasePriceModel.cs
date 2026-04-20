namespace NamEcommerce.Web.Contracts.Models.PurchaseOrders;

public sealed record RecentPurchasePriceModel(
    Guid? VendorId,
    string? VendorName,
    decimal UnitCost,
    string PurchaseOrderCode,
    DateTime PurchaseDate);
