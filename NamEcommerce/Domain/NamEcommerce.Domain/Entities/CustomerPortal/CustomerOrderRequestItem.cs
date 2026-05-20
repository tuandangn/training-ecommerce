using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerOrderRequestItem : AppAggregateEntity
{
    private CustomerOrderRequestItem() : base(Guid.NewGuid()) { }

    internal CustomerOrderRequestItem(
        Guid customerOrderRequestId,
        Guid productId,
        string productName,
        decimal quantity,
        decimal unitPriceSnapshot) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        CustomerOrderRequestId = customerOrderRequestId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPriceSnapshot = unitPriceSnapshot;
    }

    public Guid CustomerOrderRequestId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPriceSnapshot { get; private set; }
    public decimal SubTotal => Quantity * UnitPriceSnapshot;
}
