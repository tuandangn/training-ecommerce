using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Debts;

[Serializable]
public sealed record CustomerAccountBalance : AppAggregateEntity
{
    public CustomerAccountBalance(Guid id) : base(id)
    {
    }

    internal CustomerAccountBalance(Guid customerId, decimal initialAmount, DateTime occurredAtUtc) : base(Guid.NewGuid())
    {
        CustomerId = customerId;
        Balance = initialAmount;
        LastEntryOnUtc = occurredAtUtc;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
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
