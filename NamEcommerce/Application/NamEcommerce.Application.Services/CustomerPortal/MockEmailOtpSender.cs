using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class MockEmailOtpSender : ICustomerOtpSender
{
    public int Channel => (int)CustomerOtpChannel.Email;

    public Task<CustomerOtpSendResultAppDto> SendAsync(CustomerOtpSendAppDto dto)
    {
        var success = !string.IsNullOrWhiteSpace(dto.Destination);
        return Task.FromResult(new CustomerOtpSendResultAppDto
        {
            Success = success,
            ErrorMessage = success ? null : "Missing email destination"
        });
    }
}
