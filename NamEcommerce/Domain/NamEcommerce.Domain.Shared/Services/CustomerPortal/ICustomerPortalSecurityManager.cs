using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Shared.Services.CustomerPortal;

public interface ICustomerPortalSecurityManager
{
    Task<CustomerPortalAccountDto> GetOrCreateAccountAsync(Guid customerId);
    Task<CustomerPortalAccountDto?> GetAccountByCustomerIdAsync(Guid customerId);
    Task<CustomerPortalSettingsDto> GetSettingsAsync();
    Task<CustomerPortalSettingsDto> UpdateSettingsAsync(bool otpEnabled, Guid? updatedByUserId, DateTime nowUtc);
    Task SetPasswordAsync(Guid customerId, string passwordHash, string passwordSalt, bool markLoginSucceeded = true);
    Task BlockAccountAsync(Guid customerId);
    Task UnblockAccountAsync(Guid customerId);
    Task UpdateLastKnownLocationAsync(Guid customerId, UpdateCustomerPortalLocationDto dto);

    Task<CustomerOtpChallengeDto> CreateOtpChallengeAsync(CreateCustomerOtpChallengeDto dto);
    Task<CustomerOtpChallengeDto?> GetOtpChallengeByIdAsync(Guid challengeId);
    Task<CustomerOtpVerifyResultDto> VerifyOtpChallengeAsync(VerifyCustomerOtpChallengeDto dto);
    Task<bool> HasRecentOtpChallengeAsync(Guid customerId, Guid deliveryNoteId, TimeSpan cooldown, DateTime nowUtc);
    Task<int> CountSecurityEventsAsync(Guid? customerId, string? ipAddress, string eventType, CustomerPortalSecurityEventOutcome? outcome, DateTime fromUtc);

    Task<CustomerPortalSessionDto> CreateSessionAsync(CreateCustomerPortalSessionDto dto);
    Task<CustomerPortalSessionDto?> GetActiveSessionByTokenHashAsync(string sessionTokenHash, DateTime nowUtc);
    Task TouchSessionAsync(Guid sessionId, DateTime nowUtc);
    Task RevokeSessionAsync(Guid sessionId, DateTime nowUtc);

    Task<DeliveryNoteAccessTokenDto> CreateDeliveryNoteAccessTokenAsync(CreateDeliveryNoteAccessTokenDto dto);
    Task RevokeActiveDeliveryNoteAccessTokensAsync(Guid deliveryNoteId, DateTime nowUtc);
    Task<DeliveryNoteAccessTokenDto?> ResolveDeliveryNoteAccessTokenAsync(string tokenHash, DateTime nowUtc);
    Task MarkDeliveryNoteAccessTokenViewedAsync(Guid tokenId, DateTime nowUtc);

    Task<CustomerSecurityEventDto> RecordSecurityEventAsync(CreateCustomerSecurityEventDto dto);
}
