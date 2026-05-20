using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerPaymentProvider
{
    Task<CreateCustomerPaymentProviderIntentResultAppDto> CreateIntentAsync(CreateCustomerPaymentProviderIntentAppDto dto);
    Task<CustomerPaymentProviderResultAppDto> CompleteMockAsync(string providerIntentId, bool success);
}
