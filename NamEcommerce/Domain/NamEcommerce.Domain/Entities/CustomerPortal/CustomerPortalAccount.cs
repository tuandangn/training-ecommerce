using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerPortalAccount : AppAggregateEntity
{
    private CustomerPortalAccount() : base(Guid.NewGuid()) { }

    internal CustomerPortalAccount(Guid customerId) : base(Guid.NewGuid())
    {
        CustomerId = customerId;
        Status = CustomerPortalAccountStatus.Active;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? PasswordSalt { get; private set; }
    public CustomerPortalAccountStatus Status { get; private set; }
    public DateTime? PasswordSetOnUtc { get; private set; }
    public DateTime? LastLoginOnUtc { get; private set; }
    public double? LastKnownLatitude { get; private set; }
    public double? LastKnownLongitude { get; private set; }
    public double? LastKnownLocationAccuracyMeters { get; private set; }
    public DateTime? LastKnownLocationCapturedOnUtc { get; private set; }
    public string? LastKnownLocationSource { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal bool IsBlocked() => Status == CustomerPortalAccountStatus.Blocked;

    internal bool HasPassword() => !string.IsNullOrWhiteSpace(PasswordHash) && !string.IsNullOrWhiteSpace(PasswordSalt);

    internal void SetPassword(string passwordHash, string passwordSalt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordSalt);

        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        PasswordSetOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void MarkLoginSucceeded()
    {
        LastLoginOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void UpdateLastKnownLocation(double latitude, double longitude, double? accuracyMeters, string source, DateTime capturedOnUtc)
    {
        LastKnownLatitude = latitude;
        LastKnownLongitude = longitude;
        LastKnownLocationAccuracyMeters = accuracyMeters;
        LastKnownLocationCapturedOnUtc = capturedOnUtc;
        LastKnownLocationSource = source;
        UpdatedOnUtc = capturedOnUtc;
    }

    internal void Block()
    {
        Status = CustomerPortalAccountStatus.Blocked;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Unblock()
    {
        Status = CustomerPortalAccountStatus.Active;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
