using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class MockSmsOtpSender : ICustomerOtpSender
{
    public int Channel => (int)CustomerOtpChannel.Sms;

    public Task<CustomerOtpSendResultAppDto> SendAsync(CustomerOtpSendAppDto dto)
    {
        var success = !string.IsNullOrWhiteSpace(dto.Destination);
        return Task.FromResult(new CustomerOtpSendResultAppDto
        {
            Success = success,
            ErrorMessage = success ? null : "Missing SMS destination"
        });
    }
}
