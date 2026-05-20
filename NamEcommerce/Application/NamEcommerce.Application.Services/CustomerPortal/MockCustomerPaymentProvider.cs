using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class MockCustomerPaymentProvider : ICustomerPaymentProvider
{
    public Task<CreateCustomerPaymentProviderIntentResultAppDto> CreateIntentAsync(CreateCustomerPaymentProviderIntentAppDto dto)
        => Task.FromResult(new CreateCustomerPaymentProviderIntentResultAppDto
        {
            Success = true,
            ProviderIntentId = $"mock_{Guid.NewGuid():N}"
        });

    public Task<CustomerPaymentProviderResultAppDto> CompleteMockAsync(string providerIntentId, bool success)
        => Task.FromResult(new CustomerPaymentProviderResultAppDto
        {
            Success = success,
            ErrorMessage = success ? null : "Mock payment failed"
        });
}
