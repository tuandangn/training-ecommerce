using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerPortalAuthAppService
{
    Task<CustomerOtpRequestResultAppDto> RequestOtpAsync(CustomerOtpRequestAppDto dto);
    Task<CustomerPortalLoginResultAppDto> VerifyOtpAsync(CustomerOtpVerifyAppDto dto);
    Task<CustomerPortalLoginResultAppDto> PasswordLoginAsync(CustomerPasswordLoginAppDto dto);
    Task<CustomerActionResultAppDto> SetPasswordAsync(Guid customerId, SetCustomerPasswordAppDto dto);
    Task<CustomerSessionAppDto?> GetSessionAsync(string sessionToken, DateTime nowUtc);
    Task<CustomerActionResultAppDto> LogoutAsync(Guid sessionId);
}
