using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerPortalPaymentAppService
{
    Task<CustomerPaymentIntentAppDto?> CreatePaymentIntentAsync(Guid customerId, CreateCustomerPaymentIntentAppDto dto);
    Task<CustomerPaymentIntentAppDto?> CompleteMockPaymentAsync(Guid customerId, Guid paymentIntentId, bool success);
}
