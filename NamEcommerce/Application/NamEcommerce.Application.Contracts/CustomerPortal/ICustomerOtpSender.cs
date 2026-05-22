using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerOtpSender
{
    int Channel { get; }
    Task<CustomerOtpSendResultAppDto> SendAsync(CustomerOtpSendAppDto dto);
}
