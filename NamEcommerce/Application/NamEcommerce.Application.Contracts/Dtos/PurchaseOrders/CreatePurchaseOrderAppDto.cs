namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed record CreatePurchaseOrderAppDto : BasePurchaseOrderAppDto
{
    public IList<CreatePurchaseOrderItemAppDto> Items { get; init; } = [];

    public decimal TaxAmount { get; init; }
    public decimal ShippingAmount { get; init; }

    public override (bool valid, string? errorMessage) Validate()
    {
        if (Items.Count == 0)
            return (false, "Error.PurchaseOrderItemRequired");
        foreach (var item in Items)
        {
            var itemValidate = item.Validate();
            if (!itemValidate.valid)
                return itemValidate;
        }

        if (TaxAmount < 0)
            return (false, "Error.TaxAmountCannotBeNegative");
        if (ShippingAmount < 0)
            return (false, "Error.ShippingAmountCannotBeNegative");

        return base.Validate();
    }
}

[Serializable]
public sealed record CreatePurchaseOrderResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}
