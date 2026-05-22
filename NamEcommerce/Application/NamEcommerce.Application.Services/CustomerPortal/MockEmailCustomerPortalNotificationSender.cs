using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class MockEmailCustomerPortalNotificationSender : ICustomerPortalNotificationSender
{
    public int Channel => (int)CustomerOtpChannel.Email;

    public Task<CustomerPortalNotificationSendResultAppDto> SendAsync(CustomerPortalNotificationSendAppDto dto)
    {
        var success = !string.IsNullOrWhiteSpace(dto.Destination);
        return Task.FromResult(new CustomerPortalNotificationSendResultAppDto
        {
            Success = success,
            ErrorMessage = success ? null : "Missing email destination"
        });
    }
}
