using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record DeliveryNoteAccessToken : AppAggregateEntity
{
    private DeliveryNoteAccessToken() : base(Guid.NewGuid()) { }

    internal DeliveryNoteAccessToken(Guid deliveryNoteId, string tokenHash, DateTime? expiresOnUtc) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        DeliveryNoteId = deliveryNoteId;
        TokenHash = tokenHash;
        ExpiresOnUtc = expiresOnUtc;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid DeliveryNoteId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime? ExpiresOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? LastViewedOnUtc { get; private set; }

    internal bool CanUse(DateTime nowUtc)
        => RevokedOnUtc is null && (!ExpiresOnUtc.HasValue || nowUtc <= ExpiresOnUtc.Value);

    internal void MarkViewed(DateTime nowUtc)
    {
        LastViewedOnUtc = nowUtc;
    }

    internal void Revoke(DateTime nowUtc)
    {
        RevokedOnUtc ??= nowUtc;
    }
}
