using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record VendorAccountBalance : AppAggregateEntity
{
    public VendorAccountBalance(Guid id) : base(id)
    {
    }

    internal VendorAccountBalance(Guid vendorId, decimal initialAmount, DateTime occurredAtUtc) : base(Guid.NewGuid())
    {
        VendorId = vendorId;
        Balance = initialAmount;
        LastEntryOnUtc = occurredAtUtc;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid VendorId { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime? LastEntryOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void Apply(decimal delta, DateTime occurredAtUtc)
    {
        Balance += delta;
        LastEntryOnUtc = occurredAtUtc;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
