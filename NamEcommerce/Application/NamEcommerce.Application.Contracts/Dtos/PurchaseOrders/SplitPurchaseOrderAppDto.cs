namespace NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

[Serializable]
public sealed class SplitPurchaseOrderAppDto
{
    public Guid PurchaseOrderId { get; init; }
    public IList<SplitItemAppDto> Items { get; init; } = [];


    public (bool valid, string? errorMessage) Validate()
    {
        if (Items.Count == 0)
            return (false, "Error.PurchaseOrderItemRequired");

        foreach (var item in Items)
            return item.Validate();

        return (true, string.Empty);
    }

    [Serializable]
    public sealed class SplitItemAppDto
    {
        public Guid ItemId { get; init; }
        public decimal Quantity { get; init; }

        public (bool valid, string? errorMessage) Validate()
        {
            if (Quantity <= 0)
                return (false, "Error.PurchaseOrderItemQuantityMustBePositive");

            return (true, string.Empty);
        }
    }
}
