namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record UpdatePurchaseOrderAppDto(Guid Id) : BasePurchaseOrderAppDto
{
    public decimal TaxAmount { get; set; }
    public decimal ShippingAmount { get; set; }
}

[Serializable]
public sealed record UpdatePurchaseOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}
