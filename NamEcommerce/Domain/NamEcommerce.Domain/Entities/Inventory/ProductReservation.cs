using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Inventory;

[Serializable]
public sealed record ProductReservation : AppAggregateEntity
{
    internal ProductReservation(Guid productId) : base(Guid.NewGuid())
    {
        ProductId = productId;
        TotalReservedByOrder = 0;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    public Guid ProductId { get; init; }
    public decimal TotalReservedByOrder { get; internal set; }
    public DateTime UpdatedOnUtc { get; internal set; }
}
