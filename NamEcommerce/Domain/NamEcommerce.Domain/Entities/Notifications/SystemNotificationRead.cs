using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.Notifications;

[Serializable]
public sealed record SystemNotificationRead : AppEntity
{
    private SystemNotificationRead() : base(Guid.NewGuid()) { }

    internal SystemNotificationRead(Guid notificationId, Guid userId, DateTime readOnUtc) : base(Guid.NewGuid())
    {
        if (notificationId == Guid.Empty)
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        NotificationId = notificationId;
        UserId = userId;
        ReadOnUtc = readOnUtc;
    }

    public Guid NotificationId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime ReadOnUtc { get; private set; }
}
