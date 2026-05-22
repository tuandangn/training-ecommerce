using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.CustomerPortal;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;

namespace NamEcommerce.Domain.Services.CustomerPortal;

public sealed class CustomerPortalSecurityManager(
    IRepository<CustomerPortalAccount> accountRepository,
    IEntityDataReader<CustomerPortalAccount> accountReader,
    IRepository<CustomerOtpChallenge> otpChallengeRepository,
    IEntityDataReader<CustomerOtpChallenge> otpChallengeReader,
    IRepository<CustomerPortalSession> sessionRepository,
    IEntityDataReader<CustomerPortalSession> sessionReader,
    IRepository<DeliveryNoteAccessToken> accessTokenRepository,
    IEntityDataReader<DeliveryNoteAccessToken> accessTokenReader,
    IRepository<CustomerSecurityEvent> securityEventRepository,
    IEntityDataReader<CustomerSecurityEvent> securityEventReader) : ICustomerPortalSecurityManager
{
    public async Task<CustomerPortalAccountDto> GetOrCreateAccountAsync(Guid customerId)
    {
        if (customerId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.CustomerPortal.CustomerRequired");

        var existing = accountReader.DataSource.FirstOrDefault(account => account.CustomerId == customerId);
        if (existing is not null)
            return MapToDto(existing);

        var account = new CustomerPortalAccount(customerId);
        var inserted = await accountRepository.InsertAsync(account).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public Task<CustomerPortalAccountDto?> GetAccountByCustomerIdAsync(Guid customerId)
    {
        var account = accountReader.DataSource.FirstOrDefault(account => account.CustomerId == customerId);
        return Task.FromResult(account is null ? null : MapToDto(account));
    }

    public async Task SetPasswordAsync(Guid customerId, string passwordHash, string passwordSalt)
    {
        var account = accountReader.DataSource.FirstOrDefault(account => account.CustomerId == customerId)
            ?? new CustomerPortalAccount(customerId);

        account.SetPassword(passwordHash, passwordSalt);
        account.MarkLoginSucceeded();

        if (accountReader.DataSource.Any(existing => existing.Id == account.Id))
            await accountRepository.UpdateAsync(account).ConfigureAwait(false);
        else
            await accountRepository.InsertAsync(account).ConfigureAwait(false);
    }

    public async Task BlockAccountAsync(Guid customerId)
    {
        var account = accountReader.DataSource.FirstOrDefault(account => account.CustomerId == customerId)
            ?? new CustomerPortalAccount(customerId);
        account.Block();

        if (accountReader.DataSource.Any(existing => existing.Id == account.Id))
            await accountRepository.UpdateAsync(account).ConfigureAwait(false);
        else
            await accountRepository.InsertAsync(account).ConfigureAwait(false);
    }

    public async Task UnblockAccountAsync(Guid customerId)
    {
        var account = accountReader.DataSource.FirstOrDefault(account => account.CustomerId == customerId);
        if (account is null)
            return;

        account.Unblock();
        await accountRepository.UpdateAsync(account).ConfigureAwait(false);
    }

    public async Task<CustomerOtpChallengeDto> CreateOtpChallengeAsync(CreateCustomerOtpChallengeDto dto)
    {
        dto.Verify();

        var challenge = new CustomerOtpChallenge(
            dto.CustomerId,
            dto.DeliveryNoteId,
            dto.Channel,
            dto.OtpHash,
            dto.ExpiresOnUtc,
            dto.RequestedIp,
            dto.RequestedUserAgent,
            dto.SentToMasked);

        var inserted = await otpChallengeRepository.InsertAsync(challenge).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CustomerOtpChallengeDto?> GetOtpChallengeByIdAsync(Guid challengeId)
    {
        var challenge = await otpChallengeReader.GetByIdAsync(challengeId).ConfigureAwait(false);
        return challenge is null ? null : MapToDto(challenge);
    }

    public async Task<CustomerOtpVerifyResultDto> VerifyOtpChallengeAsync(VerifyCustomerOtpChallengeDto dto)
    {
        dto.Verify();

        var challenge = await otpChallengeRepository.GetByIdAsync(dto.ChallengeId).ConfigureAwait(false);
        if (challenge is null)
        {
            return new CustomerOtpVerifyResultDto
            {
                Success = false,
                CustomerId = Guid.Empty,
                DeliveryNoteId = Guid.Empty,
                Status = CustomerOtpChallengeStatus.Cancelled
            };
        }

        if (!challenge.CanVerify(dto.NowUtc))
        {
            challenge.MarkVerifyFailed(dto.NowUtc);
            await otpChallengeRepository.UpdateAsync(challenge).ConfigureAwait(false);
            return new CustomerOtpVerifyResultDto
            {
                Success = false,
                CustomerId = challenge.CustomerId,
                DeliveryNoteId = challenge.DeliveryNoteId,
                Status = challenge.Status
            };
        }

        if (!string.Equals(challenge.OtpHash, dto.OtpHash, StringComparison.Ordinal))
        {
            challenge.MarkVerifyFailed(dto.NowUtc);
            await otpChallengeRepository.UpdateAsync(challenge).ConfigureAwait(false);
            return new CustomerOtpVerifyResultDto
            {
                Success = false,
                CustomerId = challenge.CustomerId,
                DeliveryNoteId = challenge.DeliveryNoteId,
                Status = challenge.Status
            };
        }

        challenge.MarkVerified(dto.NowUtc);
        await otpChallengeRepository.UpdateAsync(challenge).ConfigureAwait(false);

        var account = accountReader.DataSource.FirstOrDefault(account => account.CustomerId == challenge.CustomerId)
            ?? new CustomerPortalAccount(challenge.CustomerId);
        account.MarkLoginSucceeded();
        if (accountReader.DataSource.Any(existing => existing.Id == account.Id))
            await accountRepository.UpdateAsync(account).ConfigureAwait(false);
        else
            await accountRepository.InsertAsync(account).ConfigureAwait(false);

        return new CustomerOtpVerifyResultDto
        {
            Success = true,
            CustomerId = challenge.CustomerId,
            DeliveryNoteId = challenge.DeliveryNoteId,
            Status = challenge.Status
        };
    }

    public Task<bool> HasRecentOtpChallengeAsync(Guid customerId, Guid deliveryNoteId, TimeSpan cooldown, DateTime nowUtc)
    {
        var cutoff = nowUtc.Subtract(cooldown);
        var hasRecent = otpChallengeReader.DataSource.Any(challenge =>
            challenge.CustomerId == customerId &&
            challenge.DeliveryNoteId == deliveryNoteId &&
            challenge.CreatedOnUtc >= cutoff &&
            challenge.Status == CustomerOtpChallengeStatus.Pending);

        return Task.FromResult(hasRecent);
    }

    public Task<int> CountSecurityEventsAsync(
        Guid? customerId,
        string? ipAddress,
        string eventType,
        CustomerPortalSecurityEventOutcome? outcome,
        DateTime fromUtc)
    {
        var query = securityEventReader.DataSource.Where(e => e.EventType == eventType && e.CreatedOnUtc >= fromUtc);
        if (customerId.HasValue)
            query = query.Where(e => e.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(ipAddress))
            query = query.Where(e => e.IpAddress == ipAddress);
        if (outcome.HasValue)
            query = query.Where(e => e.Outcome == outcome.Value);

        return Task.FromResult(query.Count());
    }

    public async Task<CustomerPortalSessionDto> CreateSessionAsync(CreateCustomerPortalSessionDto dto)
    {
        dto.Verify();

        var session = new CustomerPortalSession(
            dto.CustomerId,
            dto.SessionTokenHash,
            dto.ExpiresOnUtc,
            dto.CreatedIp,
            dto.UserAgent);

        var inserted = await sessionRepository.InsertAsync(session).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public Task<CustomerPortalSessionDto?> GetActiveSessionByTokenHashAsync(string sessionTokenHash, DateTime nowUtc)
    {
        var session = sessionReader.DataSource.FirstOrDefault(session => session.SessionTokenHash == sessionTokenHash);
        if (session is null || !session.IsActive(nowUtc))
            return Task.FromResult<CustomerPortalSessionDto?>(null);

        return Task.FromResult<CustomerPortalSessionDto?>(MapToDto(session));
    }

    public async Task TouchSessionAsync(Guid sessionId, DateTime nowUtc)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId).ConfigureAwait(false);
        if (session is null)
            return;

        session.Touch(nowUtc);
        await sessionRepository.UpdateAsync(session).ConfigureAwait(false);
    }

    public async Task RevokeSessionAsync(Guid sessionId, DateTime nowUtc)
    {
        var session = await sessionRepository.GetByIdAsync(sessionId).ConfigureAwait(false);
        if (session is null)
            return;

        session.Revoke(nowUtc);
        await sessionRepository.UpdateAsync(session).ConfigureAwait(false);
    }

    public async Task<DeliveryNoteAccessTokenDto> CreateDeliveryNoteAccessTokenAsync(CreateDeliveryNoteAccessTokenDto dto)
    {
        dto.Verify();

        var token = new DeliveryNoteAccessToken(dto.DeliveryNoteId, dto.TokenHash, dto.ExpiresOnUtc);
        var inserted = await accessTokenRepository.InsertAsync(token).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task RevokeActiveDeliveryNoteAccessTokensAsync(Guid deliveryNoteId, DateTime nowUtc)
    {
        if (deliveryNoteId == Guid.Empty)
            return;

        var tokens = accessTokenReader.DataSource
            .Where(token => token.DeliveryNoteId == deliveryNoteId && token.RevokedOnUtc == null)
            .ToList();

        foreach (var token in tokens)
        {
            token.Revoke(nowUtc);
            await accessTokenRepository.UpdateAsync(token).ConfigureAwait(false);
        }
    }

    public Task<DeliveryNoteAccessTokenDto?> ResolveDeliveryNoteAccessTokenAsync(string tokenHash, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
            return Task.FromResult<DeliveryNoteAccessTokenDto?>(null);

        var token = accessTokenReader.DataSource.FirstOrDefault(token => token.TokenHash == tokenHash);
        if (token is null || !token.CanUse(nowUtc))
            return Task.FromResult<DeliveryNoteAccessTokenDto?>(null);

        return Task.FromResult<DeliveryNoteAccessTokenDto?>(MapToDto(token));
    }

    public async Task MarkDeliveryNoteAccessTokenViewedAsync(Guid tokenId, DateTime nowUtc)
    {
        var token = await accessTokenRepository.GetByIdAsync(tokenId).ConfigureAwait(false);
        if (token is null)
            return;

        token.MarkViewed(nowUtc);
        await accessTokenRepository.UpdateAsync(token).ConfigureAwait(false);
    }

    public async Task<CustomerSecurityEventDto> RecordSecurityEventAsync(CreateCustomerSecurityEventDto dto)
    {
        dto.Verify();

        var securityEvent = new CustomerSecurityEvent(
            dto.CustomerId,
            dto.DeliveryNoteId,
            dto.EventType,
            dto.Outcome,
            dto.IpAddress,
            dto.UserAgent,
            dto.MetadataJson);

        var inserted = await securityEventRepository.InsertAsync(securityEvent).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    private static CustomerPortalAccountDto MapToDto(CustomerPortalAccount account)
        => new(account.Id)
        {
            CustomerId = account.CustomerId,
            PasswordHash = account.PasswordHash,
            PasswordSalt = account.PasswordSalt,
            Status = account.Status,
            PasswordSetOnUtc = account.PasswordSetOnUtc,
            LastLoginOnUtc = account.LastLoginOnUtc,
            CreatedOnUtc = account.CreatedOnUtc,
            UpdatedOnUtc = account.UpdatedOnUtc
        };

    private static CustomerOtpChallengeDto MapToDto(CustomerOtpChallenge challenge)
        => new(challenge.Id)
        {
            CustomerId = challenge.CustomerId,
            DeliveryNoteId = challenge.DeliveryNoteId,
            Channel = challenge.Channel,
            ExpiresOnUtc = challenge.ExpiresOnUtc,
            AttemptCount = challenge.AttemptCount,
            Status = challenge.Status,
            SentToMasked = challenge.SentToMasked,
            CreatedOnUtc = challenge.CreatedOnUtc,
            VerifiedOnUtc = challenge.VerifiedOnUtc
        };

    private static CustomerPortalSessionDto MapToDto(CustomerPortalSession session)
        => new(session.Id)
        {
            CustomerId = session.CustomerId,
            SessionTokenHash = session.SessionTokenHash,
            CreatedOnUtc = session.CreatedOnUtc,
            LastSeenOnUtc = session.LastSeenOnUtc,
            ExpiresOnUtc = session.ExpiresOnUtc,
            RevokedOnUtc = session.RevokedOnUtc
        };

    private static DeliveryNoteAccessTokenDto MapToDto(DeliveryNoteAccessToken token)
        => new(token.Id)
        {
            DeliveryNoteId = token.DeliveryNoteId,
            TokenHash = token.TokenHash,
            ExpiresOnUtc = token.ExpiresOnUtc,
            RevokedOnUtc = token.RevokedOnUtc,
            CreatedOnUtc = token.CreatedOnUtc,
            LastViewedOnUtc = token.LastViewedOnUtc
        };

    private static CustomerSecurityEventDto MapToDto(CustomerSecurityEvent securityEvent)
        => new(securityEvent.Id)
        {
            CustomerId = securityEvent.CustomerId,
            DeliveryNoteId = securityEvent.DeliveryNoteId,
            EventType = securityEvent.EventType,
            Outcome = securityEvent.Outcome,
            IpAddress = securityEvent.IpAddress,
            UserAgent = securityEvent.UserAgent,
            MetadataJson = securityEvent.MetadataJson,
            CreatedOnUtc = securityEvent.CreatedOnUtc
        };
}
