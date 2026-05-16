namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record SupplierSuggestionDto(Guid VendorId, string VendorName)
{
    public int? DisplayOrder { get; init; }
    public DateTime? LastPurchaseDateUtc { get; init; }
    public decimal? LastUnitPrice { get; init; }
    public bool IsPreferred { get; init; }
}

