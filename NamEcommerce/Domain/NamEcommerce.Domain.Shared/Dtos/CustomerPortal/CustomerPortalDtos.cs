using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Dtos.CustomerPortal;

[Serializable]
public sealed record CustomerPortalAccountDto(Guid Id)
{
    public required Guid CustomerId { get; init; }
    public string? PasswordHash { get; init; }
    public string? PasswordSalt { get; init; }
    public required CustomerPortalAccountStatus Status { get; init; }
    public DateTime? PasswordSetOnUtc { get; init; }
    public DateTime? LastLoginOnUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
}

[Serializable]
public sealed record CustomerPortalSettingsDto(Guid Id)
{
    public required bool OtpEnabled { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public Guid? UpdatedByUserId { get; init; }
}

[Serializable]
public sealed record CustomerOtpChallengeDto(Guid Id)
{
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required CustomerOtpChannel Channel { get; init; }
    public required DateTime ExpiresOnUtc { get; init; }
    public required int AttemptCount { get; init; }
    public required CustomerOtpChallengeStatus Status { get; init; }
    public string? SentToMasked { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? VerifiedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateCustomerOtpChallengeDto
{
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required CustomerOtpChannel Channel { get; init; }
    public required string OtpHash { get; init; }
    public required DateTime ExpiresOnUtc { get; init; }
    public string? RequestedIp { get; init; }
    public string? RequestedUserAgent { get; init; }
    public string? SentToMasked { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.CustomerRequired");
        if (DeliveryNoteId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.DeliveryNoteRequired");
        if (string.IsNullOrWhiteSpace(OtpHash))
            throw new NamEcommerceDomainException("Error.CustomerPortal.OtpHashRequired");
        if (ExpiresOnUtc <= DateTime.UtcNow)
            throw new NamEcommerceDomainException("Error.CustomerPortal.OtpExpiryInvalid");
    }
}

[Serializable]
public sealed record VerifyCustomerOtpChallengeDto(Guid ChallengeId)
{
    public required string OtpHash { get; init; }
    public required DateTime NowUtc { get; init; }

    public void Verify()
    {
        if (ChallengeId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.OtpChallengeRequired");
        if (string.IsNullOrWhiteSpace(OtpHash))
            throw new NamEcommerceDomainException("Error.CustomerPortal.OtpRequired");
    }
}

[Serializable]
public sealed record CustomerOtpVerifyResultDto
{
    public required bool Success { get; init; }
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required CustomerOtpChallengeStatus Status { get; init; }
}

[Serializable]
public sealed record CustomerPortalSessionDto(Guid Id)
{
    public required Guid CustomerId { get; init; }
    public required string SessionTokenHash { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime LastSeenOnUtc { get; init; }
    public DateTime ExpiresOnUtc { get; init; }
    public DateTime? RevokedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateCustomerPortalSessionDto
{
    public required Guid CustomerId { get; init; }
    public required string SessionTokenHash { get; init; }
    public required DateTime ExpiresOnUtc { get; init; }
    public string? CreatedIp { get; init; }
    public string? UserAgent { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.CustomerRequired");
        if (string.IsNullOrWhiteSpace(SessionTokenHash))
            throw new NamEcommerceDomainException("Error.CustomerPortal.SessionTokenRequired");
        if (ExpiresOnUtc <= DateTime.UtcNow)
            throw new NamEcommerceDomainException("Error.CustomerPortal.SessionExpiryInvalid");
    }
}

[Serializable]
public sealed record DeliveryNoteAccessTokenDto(Guid Id)
{
    public required Guid DeliveryNoteId { get; init; }
    public required string TokenHash { get; init; }
    public DateTime? ExpiresOnUtc { get; init; }
    public DateTime? RevokedOnUtc { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? LastViewedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateDeliveryNoteAccessTokenDto
{
    public required Guid DeliveryNoteId { get; init; }
    public required string TokenHash { get; init; }
    public DateTime? ExpiresOnUtc { get; init; }

    public void Verify()
    {
        if (DeliveryNoteId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.DeliveryNoteRequired");
        if (string.IsNullOrWhiteSpace(TokenHash))
            throw new NamEcommerceDomainException("Error.CustomerPortal.DeliveryTokenRequired");
    }
}

[Serializable]
public sealed record CreateCustomerSecurityEventDto
{
    public Guid? CustomerId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public required string EventType { get; init; }
    public required CustomerPortalSecurityEventOutcome Outcome { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? MetadataJson { get; init; }

    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(EventType))
            throw new NamEcommerceDomainException("Error.CustomerPortal.SecurityEventTypeRequired");
    }
}

[Serializable]
public sealed record CustomerSecurityEventDto(Guid Id)
{
    public Guid? CustomerId { get; init; }
    public Guid? DeliveryNoteId { get; init; }
    public required string EventType { get; init; }
    public required CustomerPortalSecurityEventOutcome Outcome { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? MetadataJson { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateCustomerDeliveryFeedbackDto
{
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public int? Rating { get; init; }
    public string? Message { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.CustomerRequired");
        if (DeliveryNoteId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.DeliveryNoteRequired");
        if (Rating is < 1 or > 5)
            throw new NamEcommerceDomainException("Error.CustomerPortal.RatingInvalid");
    }
}

[Serializable]
public sealed record CustomerDeliveryFeedbackDto(Guid Id)
{
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public int? Rating { get; init; }
    public string? Message { get; init; }
    public required CustomerDeliveryFeedbackStatus Status { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ReviewedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateCustomerOrderRequestDto
{
    public required Guid CustomerId { get; init; }
    public DateTime? ExpectedShippingDateUtc { get; init; }
    public string? ShippingAddress { get; init; }
    public string? Note { get; init; }
    public required IList<CreateCustomerOrderRequestItemDto> Items { get; init; } = [];

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.CustomerRequired");
        if (Items.Count == 0)
            throw new NamEcommerceDomainException("Error.CustomerPortal.OrderRequestItemsRequired");
        if (Items.Any(item => item.ProductId == Guid.Empty))
            throw new NamEcommerceDomainException("Error.CustomerPortal.ProductRequired");
        if (Items.Any(item => item.Quantity <= 0))
            throw new NamEcommerceDomainException("Error.CustomerPortal.QuantityMustBePositive");
    }
}

[Serializable]
public sealed record CreateCustomerOrderRequestItemDto
{
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPriceSnapshot { get; init; }
}

[Serializable]
public sealed record CustomerOrderRequestDto(Guid Id)
{
    public required Guid CustomerId { get; init; }
    public required string Code { get; init; }
    public required CustomerOrderRequestStatus Status { get; init; }
    public DateTime? ExpectedShippingDateUtc { get; init; }
    public string? ShippingAddress { get; init; }
    public string? Note { get; init; }
    public string? AdminNote { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ReviewedOnUtc { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public Guid? ConvertedOrderId { get; init; }
    public IList<CustomerOrderRequestItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerOrderRequestItemDto(Guid Id)
{
    public required Guid CustomerOrderRequestId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal UnitPriceSnapshot { get; init; }
    public required decimal SubTotal { get; init; }
}

[Serializable]
public sealed record CreateCustomerReturnRequestDto
{
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public string? Reason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public required IList<CreateCustomerReturnRequestItemDto> Items { get; init; } = [];

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.CustomerRequired");
        if (DeliveryNoteId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.DeliveryNoteRequired");
        if (Items.Count == 0)
            throw new NamEcommerceDomainException("Error.CustomerPortal.ReturnRequestItemsRequired");
        if (Items.Any(item => item.DeliveryNoteItemId == Guid.Empty || item.ProductId == Guid.Empty))
            throw new NamEcommerceDomainException("Error.CustomerPortal.ReturnRequestItemInvalid");
        if (Items.Any(item => item.RequestedQuantity <= 0))
            throw new NamEcommerceDomainException("Error.CustomerPortal.QuantityMustBePositive");
        if (Items.Any(item => item.EvidencePictureIds.Any(pictureId => pictureId == Guid.Empty)))
            throw new NamEcommerceDomainException("Error.CustomerPortal.ReturnEvidencePictureInvalid");
    }
}

[Serializable]
public sealed record CreateCustomerReturnRequestItemDto
{
    public required Guid DeliveryNoteItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public string? Reason { get; init; }
    public IList<Guid> EvidencePictureIds { get; init; } = [];
}

[Serializable]
public sealed record CustomerReturnRequestDto(Guid Id)
{
    public required Guid CustomerId { get; init; }
    public required Guid DeliveryNoteId { get; init; }
    public required CustomerReturnRequestStatus Status { get; init; }
    public string? Reason { get; init; }
    public bool CompensateInNextDelivery { get; init; }
    public string? AdminNote { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? ReviewedOnUtc { get; init; }
    public Guid? ReviewedByUserId { get; init; }
    public Guid? ConvertedCustomerReturnId { get; init; }
    public IList<CustomerReturnRequestItemDto> Items { get; init; } = [];
}

[Serializable]
public sealed record CustomerReturnRequestItemDto(Guid Id)
{
    public required Guid CustomerReturnRequestId { get; init; }
    public required Guid DeliveryNoteItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public string? Reason { get; init; }
    public IList<CustomerReturnRequestItemPictureDto> EvidencePictures { get; init; } = [];
}

[Serializable]
public sealed record CustomerReturnRequestItemPictureDto(Guid Id)
{
    public required Guid CustomerReturnRequestItemId { get; init; }
    public required Guid PictureId { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record CreateCustomerPaymentIntentDto
{
    public required Guid CustomerId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public required decimal Amount { get; init; }
    public required string Provider { get; init; }

    public void Verify()
    {
        if (CustomerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.CustomerRequired");
        if (Amount <= 0)
            throw new NamEcommerceDomainException("Error.CustomerPortal.PaymentAmountMustBePositive");
        if (string.IsNullOrWhiteSpace(Provider))
            throw new NamEcommerceDomainException("Error.CustomerPortal.PaymentProviderRequired");
    }
}

[Serializable]
public sealed record CustomerPaymentIntentDto(Guid Id)
{
    public required Guid CustomerId { get; init; }
    public Guid? CustomerDebtId { get; init; }
    public required decimal Amount { get; init; }
    public required string Provider { get; init; }
    public string? ProviderIntentId { get; init; }
    public required CustomerPaymentIntentStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? CompletedOnUtc { get; init; }
    public DateTime? ReconciledOnUtc { get; init; }
    public Guid? ReconciledByUserId { get; init; }
    public Guid? CustomerPaymentId { get; init; }
}
