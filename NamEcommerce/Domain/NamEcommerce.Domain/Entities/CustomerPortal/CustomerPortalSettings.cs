using NamEcommerce.Domain.Shared;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerPortalSettings : AppAggregateEntity
{
    private CustomerPortalSettings() : base(Guid.NewGuid()) { }

    internal CustomerPortalSettings(bool otpEnabled) : base(Guid.NewGuid())
    {
        OtpEnabled = otpEnabled;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public bool OtpEnabled { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }

    internal void UpdateOtpEnabled(bool otpEnabled, Guid? updatedByUserId, DateTime nowUtc)
    {
        OtpEnabled = otpEnabled;
        UpdatedByUserId = updatedByUserId;
        UpdatedOnUtc = nowUtc;
    }
}
