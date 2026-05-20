using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerSecurityEvent : AppAggregateEntity
{
    private CustomerSecurityEvent() : base(Guid.NewGuid()) { }

    internal CustomerSecurityEvent(
        Guid? customerId,
        Guid? deliveryNoteId,
        string eventType,
        CustomerPortalSecurityEventOutcome outcome,
        string? ipAddress,
        string? userAgent,
        string? metadataJson) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        CustomerId = customerId;
        DeliveryNoteId = deliveryNoteId;
        EventType = eventType;
        Outcome = outcome;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        MetadataJson = metadataJson;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid? CustomerId { get; private set; }
    public Guid? DeliveryNoteId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public CustomerPortalSecurityEventOutcome Outcome { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? MetadataJson { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
}
