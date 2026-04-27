namespace NamEcommerce.Domain.Shared.Events;

/// <summary>
/// Base class cho mọi Domain Event.
/// Concrete event nên là <c>sealed record</c> kế thừa class này, mang theo các giá trị cần thiết để handler xử lý.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public Guid EventId { get; init; }

    /// <inheritdoc />
    public DateTime OccurredOnUtc { get; init; }
}
