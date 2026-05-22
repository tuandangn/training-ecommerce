using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Entities.CustomerPortal;

[Serializable]
public sealed record CustomerOtpChallenge : AppAggregateEntity
{
    public const int MaxAttempts = 5;

    private CustomerOtpChallenge() : base(Guid.NewGuid()) { }

    internal CustomerOtpChallenge(
        Guid customerId,
        Guid deliveryNoteId,
        CustomerOtpChannel channel,
        string otpHash,
        DateTime expiresOnUtc,
        string? requestedIp,
        string? requestedUserAgent,
        string? sentToMasked) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(otpHash);

        CustomerId = customerId;
        DeliveryNoteId = deliveryNoteId;
        Channel = channel;
        OtpHash = otpHash;
        ExpiresOnUtc = expiresOnUtc;
        RequestedIp = requestedIp;
        RequestedUserAgent = requestedUserAgent;
        SentToMasked = sentToMasked;
        Status = CustomerOtpChallengeStatus.Pending;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public Guid DeliveryNoteId { get; private set; }
    public CustomerOtpChannel Channel { get; private set; }
    public string OtpHash { get; private set; } = string.Empty;
    public DateTime ExpiresOnUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public CustomerOtpChallengeStatus Status { get; private set; }
    public string? RequestedIp { get; private set; }
    public string? RequestedUserAgent { get; private set; }
    public string? SentToMasked { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? VerifiedOnUtc { get; private set; }

    internal bool CanVerify(DateTime nowUtc)
        => Status == CustomerOtpChallengeStatus.Pending
            && nowUtc <= ExpiresOnUtc
            && AttemptCount < MaxAttempts;

    internal void MarkVerifyFailed(DateTime nowUtc)
    {
        AttemptCount++;
        if (nowUtc > ExpiresOnUtc)
        {
            Status = CustomerOtpChallengeStatus.Expired;
            return;
        }

        if (AttemptCount >= MaxAttempts)
            Status = CustomerOtpChallengeStatus.Locked;
    }

    internal void MarkVerified(DateTime nowUtc)
    {
        Status = CustomerOtpChallengeStatus.Verified;
        VerifiedOnUtc = nowUtc;
    }
}
