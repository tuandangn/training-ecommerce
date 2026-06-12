using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.Security;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class CustomerPortalAuthAppService(
    ICustomerPortalSecurityManager securityManager,
    IDeliveryNoteAppService deliveryNoteAppService,
    IEntityDataReader<Customer> customerReader,
    ISecurityService securityService,
    IEnumerable<ICustomerOtpSender> otpSenders,
    CustomerPortalSecurityOptions securityOptions) : ICustomerPortalAuthAppService
{
    private const string OtpRequestEvent = "OtpRequest";
    private const string OtpVerifyEvent = "OtpVerify";
    private const string OtpDisabledLoginEvent = "OtpDisabledLogin";
    private const string PasswordLoginEvent = "PasswordLogin";
    private const string SetPasswordEvent = "SetPassword";
    private const string ChangePasswordEvent = "ChangePassword";

    private static readonly CustomerOtpRequestResultAppDto GenericOtpFailure = new()
    {
        Success = false,
        Message = "Không thể gửi OTP lúc này. Vui lòng thử lại sau."
    };

    private static readonly CustomerPortalLoginResultAppDto GenericLoginFailure = new()
    {
        Success = false,
        Message = "Thông tin xác thực không hợp lệ hoặc đã bị giới hạn tạm thời."
    };

    public async Task<CustomerOtpRequestResultAppDto> RequestOtpAsync(CustomerOtpRequestAppDto dto)
    {
        var nowUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(dto.DeliveryToken))
        {
            await RecordEventAsync(null, null, OtpRequestEvent, CustomerPortalSecurityEventOutcome.Failed, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericOtpFailure;
        }

        var tokenHash = CustomerPortalHashing.Hash(dto.DeliveryToken);
        var token = await securityManager.ResolveDeliveryNoteAccessTokenAsync(tokenHash, nowUtc).ConfigureAwait(false);
        if (token is null)
            return GenericOtpFailure;

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(token.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return GenericOtpFailure;

        var account = await securityManager.GetOrCreateAccountAsync(deliveryNote.CustomerId).ConfigureAwait(false);
        if (account.Status == CustomerPortalAccountStatus.Blocked)
        {
            await RecordEventAsync(deliveryNote.CustomerId, deliveryNote.Id, OtpRequestEvent, CustomerPortalSecurityEventOutcome.Blocked, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericOtpFailure;
        }

        var settings = await securityManager.GetSettingsAsync().ConfigureAwait(false);
        if (!settings.OtpEnabled)
        {
            await securityManager.MarkDeliveryNoteAccessTokenViewedAsync(token.Id, nowUtc).ConfigureAwait(false);
            await RecordEventAsync(deliveryNote.CustomerId, deliveryNote.Id, OtpDisabledLoginEvent, CustomerPortalSecurityEventOutcome.Succeeded, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            await UpdateCustomerLocationAsync(deliveryNote.CustomerId, dto.Location, OtpDisabledLoginEvent).ConfigureAwait(false);
            var loginResult = await CreateLoginResultAsync(deliveryNote.CustomerId, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return new CustomerOtpRequestResultAppDto
            {
                Success = loginResult.Success,
                Message = "OTP đang tắt. Khách hàng đã được xác thực bằng mã QR phiếu giao.",
                RequiresOtp = false,
                SessionToken = loginResult.SessionToken,
                Session = loginResult.Session
            };
        }

        if (await securityManager.HasRecentOtpChallengeAsync(deliveryNote.CustomerId, deliveryNote.Id, TimeSpan.FromSeconds(securityOptions.SafeOtpCooldownSeconds), nowUtc).ConfigureAwait(false) ||
            await IsOtpRateLimitedAsync(deliveryNote.CustomerId, dto.RequestedIp, nowUtc).ConfigureAwait(false))
        {
            await RecordEventAsync(deliveryNote.CustomerId, deliveryNote.Id, OtpRequestEvent, CustomerPortalSecurityEventOutcome.Blocked, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericOtpFailure;
        }

        var customer = await customerReader.GetByIdAsync(deliveryNote.CustomerId, default).ConfigureAwait(false);
        var otp = CustomerPortalHashing.GenerateOtp();
        var sent = await TrySendOtpAsync(deliveryNote.CustomerPhone, customer?.Email, otp).ConfigureAwait(false);
        if (sent.Channel is null)
        {
            await RecordEventAsync(deliveryNote.CustomerId, deliveryNote.Id, OtpRequestEvent, CustomerPortalSecurityEventOutcome.Failed, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericOtpFailure;
        }

        var challenge = await securityManager.CreateOtpChallengeAsync(new CreateCustomerOtpChallengeDto
        {
            CustomerId = deliveryNote.CustomerId,
            DeliveryNoteId = deliveryNote.Id,
            Channel = sent.Channel.Value,
            OtpHash = CustomerPortalHashing.Hash(otp),
            ExpiresOnUtc = nowUtc.AddMinutes(securityOptions.SafeOtpExpiryMinutes),
            RequestedIp = dto.RequestedIp,
            RequestedUserAgent = dto.RequestedUserAgent,
            SentToMasked = sent.MaskedDestination
        }).ConfigureAwait(false);

        await securityManager.MarkDeliveryNoteAccessTokenViewedAsync(token.Id, nowUtc).ConfigureAwait(false);
        await RecordEventAsync(deliveryNote.CustomerId, deliveryNote.Id, OtpRequestEvent, CustomerPortalSecurityEventOutcome.Succeeded, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);

        return new CustomerOtpRequestResultAppDto
        {
            Success = true,
            Message = "OTP đã được gửi.",
            ChallengeId = challenge.Id,
            MaskedDestination = sent.MaskedDestination,
            MockOtp = otp
        };
    }

    public async Task<CustomerPortalLoginResultAppDto> VerifyOtpAsync(CustomerOtpVerifyAppDto dto)
    {
        var nowUtc = DateTime.UtcNow;
        if (dto.ChallengeId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Otp))
        {
            await RecordEventAsync(null, null, OtpVerifyEvent, CustomerPortalSecurityEventOutcome.Failed, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericLoginFailure;
        }

        var result = await securityManager.VerifyOtpChallengeAsync(new VerifyCustomerOtpChallengeDto(dto.ChallengeId)
        {
            OtpHash = CustomerPortalHashing.Hash(dto.Otp),
            NowUtc = nowUtc
        }).ConfigureAwait(false);

        if (!result.Success)
        {
            await RecordEventAsync(result.CustomerId == Guid.Empty ? null : result.CustomerId, result.DeliveryNoteId == Guid.Empty ? null : result.DeliveryNoteId, OtpVerifyEvent, CustomerPortalSecurityEventOutcome.Failed, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericLoginFailure;
        }

        await RecordEventAsync(result.CustomerId, result.DeliveryNoteId, OtpVerifyEvent, CustomerPortalSecurityEventOutcome.Succeeded, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
        await UpdateCustomerLocationAsync(result.CustomerId, dto.Location, OtpVerifyEvent).ConfigureAwait(false);
        return await CreateLoginResultAsync(result.CustomerId, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
    }

    public async Task<CustomerPortalLoginResultAppDto> PasswordLoginAsync(CustomerPasswordLoginAppDto dto)
    {
        var nowUtc = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
        {
            await RecordEventAsync(null, null, PasswordLoginEvent, CustomerPortalSecurityEventOutcome.Failed, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericLoginFailure;
        }

        var login = dto.Login.Trim();
        var customer = customerReader.DataSource.FirstOrDefault(customer =>
            customer.PhoneNumber == login ||
            (customer.Email != null && customer.Email.ToUpper() == login.ToUpper()));

        if (await IsPasswordLoginRateLimitedAsync(customer?.Id, dto.RequestedIp, nowUtc).ConfigureAwait(false))
        {
            await RecordEventAsync(customer?.Id, null, PasswordLoginEvent, CustomerPortalSecurityEventOutcome.Blocked, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericLoginFailure;
        }

        if (customer is null)
        {
            await RecordEventAsync(null, null, PasswordLoginEvent, CustomerPortalSecurityEventOutcome.Failed, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericLoginFailure;
        }

        var account = await securityManager.GetAccountByCustomerIdAsync(customer.Id).ConfigureAwait(false);
        if (account is null || account.Status == CustomerPortalAccountStatus.Blocked ||
            string.IsNullOrWhiteSpace(account.PasswordHash) || string.IsNullOrWhiteSpace(account.PasswordSalt))
        {
            await RecordEventAsync(customer.Id, null, PasswordLoginEvent, CustomerPortalSecurityEventOutcome.Failed, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericLoginFailure;
        }

        var valid = await securityService.VerifyPasswordAsync(dto.Password, account.PasswordHash, account.PasswordSalt).ConfigureAwait(false);
        if (!valid)
        {
            await RecordEventAsync(customer.Id, null, PasswordLoginEvent, CustomerPortalSecurityEventOutcome.Failed, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
            return GenericLoginFailure;
        }

        await RecordEventAsync(customer.Id, null, PasswordLoginEvent, CustomerPortalSecurityEventOutcome.Succeeded, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
        return await CreateLoginResultAsync(customer.Id, dto.RequestedIp, dto.RequestedUserAgent).ConfigureAwait(false);
    }

    public async Task<CustomerActionResultAppDto> SetPasswordAsync(Guid customerId, SetCustomerPasswordAppDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            return CustomerActionResultAppDto.Fail("Mật khẩu cần tối thiểu 8 ký tự.");

        var account = await securityManager.GetAccountByCustomerIdAsync(customerId).ConfigureAwait(false);
        if (account?.Status == CustomerPortalAccountStatus.Blocked)
            return CustomerActionResultAppDto.Fail("Tài khoản đã bị khóa.");

        var hash = await securityService.HashPasswordAsync(dto.Password).ConfigureAwait(false);
        await securityManager.SetPasswordAsync(customerId, hash.PasswordHash, hash.PasswordSalt).ConfigureAwait(false);
        await RecordEventAsync(customerId, null, SetPasswordEvent, CustomerPortalSecurityEventOutcome.Succeeded, null, null).ConfigureAwait(false);
        return CustomerActionResultAppDto.Ok("Đã thiết lập mật khẩu.");
    }

    public async Task<CustomerActionResultAppDto> ChangePasswordAsync(Guid customerId, ChangeCustomerPasswordAppDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CurrentPassword))
            return CustomerActionResultAppDto.Fail("Vui lòng nhập mật khẩu hiện tại.");
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
            return CustomerActionResultAppDto.Fail("Mật khẩu mới cần tối thiểu 8 ký tự.");

        var account = await securityManager.GetAccountByCustomerIdAsync(customerId).ConfigureAwait(false);
        if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash) || string.IsNullOrWhiteSpace(account.PasswordSalt))
            return CustomerActionResultAppDto.Fail("Tài khoản chưa thiết lập mật khẩu.");

        if (account.Status == CustomerPortalAccountStatus.Blocked)
            return CustomerActionResultAppDto.Fail("Tài khoản đã bị khóa.");

        var currentPasswordValid = await securityService.VerifyPasswordAsync(dto.CurrentPassword, account.PasswordHash, account.PasswordSalt).ConfigureAwait(false);
        if (!currentPasswordValid)
        {
            await RecordEventAsync(customerId, null, ChangePasswordEvent, CustomerPortalSecurityEventOutcome.Failed, null, null).ConfigureAwait(false);
            return CustomerActionResultAppDto.Fail("Mật khẩu hiện tại không đúng.");
        }

        var hash = await securityService.HashPasswordAsync(dto.NewPassword).ConfigureAwait(false);
        await securityManager.SetPasswordAsync(customerId, hash.PasswordHash, hash.PasswordSalt).ConfigureAwait(false);
        await RecordEventAsync(customerId, null, ChangePasswordEvent, CustomerPortalSecurityEventOutcome.Succeeded, null, null).ConfigureAwait(false);
        return CustomerActionResultAppDto.Ok("Đã đổi mật khẩu.");
    }

    public async Task<CustomerSessionAppDto?> GetSessionAsync(string sessionToken, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(sessionToken))
            return null;

        var session = await securityManager.GetActiveSessionByTokenHashAsync(CustomerPortalHashing.Hash(sessionToken), nowUtc).ConfigureAwait(false);
        if (session is null)
            return null;

        var account = await securityManager.GetAccountByCustomerIdAsync(session.CustomerId).ConfigureAwait(false);
        if (account?.Status == CustomerPortalAccountStatus.Blocked)
            return null;

        await securityManager.TouchSessionAsync(session.Id, nowUtc).ConfigureAwait(false);
        return await MapSessionAsync(session, account).ConfigureAwait(false);
    }

    public async Task<CustomerActionResultAppDto> LogoutAsync(Guid sessionId)
    {
        await securityManager.RevokeSessionAsync(sessionId, DateTime.UtcNow).ConfigureAwait(false);
        return CustomerActionResultAppDto.Ok();
    }

    private async Task<CustomerPortalLoginResultAppDto> CreateLoginResultAsync(Guid customerId, string? ip, string? userAgent)
    {
        var rawToken = CustomerPortalHashing.CreateSecureToken();
        var session = await securityManager.CreateSessionAsync(new CreateCustomerPortalSessionDto
        {
            CustomerId = customerId,
            SessionTokenHash = CustomerPortalHashing.Hash(rawToken),
            ExpiresOnUtc = DateTime.UtcNow.AddHours(securityOptions.SafeSessionExpiryHours),
            CreatedIp = ip,
            UserAgent = userAgent
        }).ConfigureAwait(false);

        var account = await securityManager.GetAccountByCustomerIdAsync(customerId).ConfigureAwait(false);
        return new CustomerPortalLoginResultAppDto
        {
            Success = true,
            SessionToken = rawToken,
            Session = await MapSessionAsync(session, account).ConfigureAwait(false)
        };
    }

    private async Task<CustomerSessionAppDto?> MapSessionAsync(CustomerPortalSessionDto session, CustomerPortalAccountDto? account)
    {
        var customer = await customerReader.GetByIdAsync(session.CustomerId, default).ConfigureAwait(false);
        if (customer is null)
            return null;

        return new CustomerSessionAppDto
        {
            SessionId = session.Id,
            CustomerId = session.CustomerId,
            CustomerName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            HasPassword = !string.IsNullOrWhiteSpace(account?.PasswordHash),
            ExpiresOnUtc = session.ExpiresOnUtc
        };
    }

    private async Task<bool> IsOtpRateLimitedAsync(Guid customerId, string? ipAddress, DateTime nowUtc)
    {
        var fromUtc = nowUtc.AddHours(-1);
        var byCustomer = await securityManager.CountSecurityEventsAsync(customerId, null, OtpRequestEvent, null, fromUtc).ConfigureAwait(false);
        if (byCustomer >= securityOptions.SafeMaxOtpRequestsPerCustomerPerHour)
            return true;

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            var byIp = await securityManager.CountSecurityEventsAsync(null, ipAddress, OtpRequestEvent, null, fromUtc).ConfigureAwait(false);
            if (byIp >= securityOptions.SafeMaxOtpRequestsPerIpPerHour)
                return true;
        }

        return false;
    }

    private async Task<bool> IsPasswordLoginRateLimitedAsync(Guid? customerId, string? ipAddress, DateTime nowUtc)
    {
        var fromUtc = nowUtc.AddHours(-1);
        if (customerId.HasValue)
        {
            var byCustomer = await securityManager.CountSecurityEventsAsync(customerId, null, PasswordLoginEvent, CustomerPortalSecurityEventOutcome.Failed, fromUtc).ConfigureAwait(false);
            if (byCustomer >= securityOptions.SafeMaxPasswordFailuresPerCustomerPerHour)
                return true;
        }

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            var byIp = await securityManager.CountSecurityEventsAsync(null, ipAddress, PasswordLoginEvent, CustomerPortalSecurityEventOutcome.Failed, fromUtc).ConfigureAwait(false);
            if (byIp >= securityOptions.SafeMaxPasswordFailuresPerIpPerHour)
                return true;
        }

        return false;
    }

    private async Task<(CustomerOtpChannel? Channel, string? MaskedDestination)> TrySendOtpAsync(string? phoneNumber, string? email, string otp)
    {
        var smsSender = otpSenders.FirstOrDefault(sender => sender.Channel == (int)CustomerOtpChannel.Sms);
        if (smsSender is not null && !string.IsNullOrWhiteSpace(phoneNumber))
        {
            var smsResult = await smsSender.SendAsync(new CustomerOtpSendAppDto
            {
                Channel = (int)CustomerOtpChannel.Sms,
                Destination = phoneNumber,
                Otp = otp
            }).ConfigureAwait(false);

            if (smsResult.Success)
                return (CustomerOtpChannel.Sms, MaskPhone(phoneNumber));
        }

        var emailSender = otpSenders.FirstOrDefault(sender => sender.Channel == (int)CustomerOtpChannel.Email);
        if (emailSender is not null && !string.IsNullOrWhiteSpace(email))
        {
            var emailResult = await emailSender.SendAsync(new CustomerOtpSendAppDto
            {
                Channel = (int)CustomerOtpChannel.Email,
                Destination = email,
                Otp = otp
            }).ConfigureAwait(false);

            if (emailResult.Success)
                return (CustomerOtpChannel.Email, MaskEmail(email));
        }

        return (null, null);
    }

    private Task RecordEventAsync(Guid? customerId, Guid? deliveryNoteId, string eventType, CustomerPortalSecurityEventOutcome outcome, string? ip, string? userAgent)
        => securityManager.RecordSecurityEventAsync(new CreateCustomerSecurityEventDto
        {
            CustomerId = customerId,
            DeliveryNoteId = deliveryNoteId,
            EventType = eventType,
            Outcome = outcome,
            IpAddress = ip,
            UserAgent = userAgent
        });

    private Task UpdateCustomerLocationAsync(Guid customerId, CustomerPortalLocationAppDto? location, string source)
    {
        if (location is null)
            return Task.CompletedTask;

        return securityManager.UpdateLastKnownLocationAsync(customerId, new UpdateCustomerPortalLocationDto
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AccuracyMeters = location.AccuracyMeters,
            Source = source,
            CapturedOnUtc = DateTime.UtcNow
        });
    }

    private static string MaskPhone(string phoneNumber)
    {
        var trimmed = phoneNumber.Trim();
        return trimmed.Length <= 4 ? "****" : $"{new string('*', Math.Max(0, trimmed.Length - 4))}{trimmed[^4..]}";
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);
        if (parts.Length != 2)
            return "***";

        var prefix = parts[0].Length <= 2 ? $"{parts[0][0]}*" : $"{parts[0][0]}***{parts[0][^1]}";
        return $"{prefix}@{parts[1]}";
    }
}
