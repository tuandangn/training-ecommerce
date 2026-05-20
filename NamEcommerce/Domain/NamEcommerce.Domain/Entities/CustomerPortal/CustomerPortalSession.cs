using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerPortalSession : AppAggregateEntity
{
    private CustomerPortalSession() : base(Guid.NewGuid()) { }

    internal CustomerPortalSession(
        Guid customerId,
        string sessionTokenHash,
        DateTime expiresOnUtc,
        string? createdIp,
        string? userAgent) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionTokenHash);

        CustomerId = customerId;
        SessionTokenHash = sessionTokenHash;
        ExpiresOnUtc = expiresOnUtc;
        CreatedIp = createdIp;
        UserAgent = userAgent;
        CreatedOnUtc = DateTime.UtcNow;
        LastSeenOnUtc = CreatedOnUtc;
    }

    public Guid CustomerId { get; private set; }
    public string SessionTokenHash { get; private set; } = string.Empty;
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime LastSeenOnUtc { get; private set; }
    public DateTime ExpiresOnUtc { get; private set; }
    public DateTime? RevokedOnUtc { get; private set; }
    public string? CreatedIp { get; private set; }
    public string? UserAgent { get; private set; }

    internal bool IsActive(DateTime nowUtc) => RevokedOnUtc is null && nowUtc <= ExpiresOnUtc;

    internal void Touch(DateTime nowUtc)
    {
        if (IsActive(nowUtc))
            LastSeenOnUtc = nowUtc;
    }

    internal void Revoke(DateTime nowUtc)
    {
        RevokedOnUtc ??= nowUtc;
    }
}
