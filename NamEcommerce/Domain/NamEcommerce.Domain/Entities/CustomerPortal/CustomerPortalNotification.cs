using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerPortalNotification : AppAggregateEntity
{
    private CustomerPortalNotification() : base(Guid.NewGuid()) { }

    internal CustomerPortalNotification(
        Guid customerId,
        CustomerPortalNotificationType type,
        string title,
        string? message,
        Guid? relatedEntityId,
        string? relatedEntityType) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        CustomerId = customerId;
        Type = type;
        Title = title;
        Message = message;
        RelatedEntityId = relatedEntityId;
        RelatedEntityType = relatedEntityType;
        Status = CustomerPortalNotificationStatus.Unread;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public CustomerPortalNotificationType Type { get; private set; }
    public CustomerPortalNotificationStatus Status { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Message { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ReadOnUtc { get; private set; }
    public Guid? ReadByUserId { get; private set; }

    internal void MarkRead(Guid readByUserId, DateTime nowUtc)
    {
        if (Status == CustomerPortalNotificationStatus.Read)
            return;

        Status = CustomerPortalNotificationStatus.Read;
        ReadByUserId = readByUserId;
        ReadOnUtc = nowUtc;
    }
}
