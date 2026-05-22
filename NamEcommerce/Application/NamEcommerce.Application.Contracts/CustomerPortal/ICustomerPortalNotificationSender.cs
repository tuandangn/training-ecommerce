using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerPortalNotificationSender
{
    int Channel { get; }
    Task<CustomerPortalNotificationSendResultAppDto> SendAsync(CustomerPortalNotificationSendAppDto dto);
}
