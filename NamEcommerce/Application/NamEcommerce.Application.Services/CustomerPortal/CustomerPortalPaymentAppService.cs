using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class CustomerPortalPaymentAppService(
    ICustomerPortalManager customerPortalManager,
    ICustomerPaymentProvider paymentProvider,
    ICustomerDebtAppService customerDebtAppService) : ICustomerPortalPaymentAppService
{
    private const string MockProviderName = "Mock";

    public async Task<CustomerPaymentIntentAppDto?> CreatePaymentIntentAsync(Guid customerId, CreateCustomerPaymentIntentAppDto dto)
    {
        if (dto.Amount <= 0)
            return null;

        if (dto.CustomerDebtId.HasValue)
        {
            var debt = await customerDebtAppService.GetDebtByIdAsync(dto.CustomerDebtId.Value).ConfigureAwait(false);
            if (debt is null || debt.CustomerId != customerId || dto.Amount > debt.RemainingAmount)
                return null;
        }

        var intent = await customerPortalManager.CreatePaymentIntentAsync(new CreateCustomerPaymentIntentDto
        {
            CustomerId = customerId,
            CustomerDebtId = dto.CustomerDebtId,
            Amount = dto.Amount,
            Provider = MockProviderName
        }).ConfigureAwait(false);

        var providerIntent = await paymentProvider.CreateIntentAsync(new CreateCustomerPaymentProviderIntentAppDto
        {
            PaymentIntentId = intent.Id,
            Amount = dto.Amount
        }).ConfigureAwait(false);

        if (!providerIntent.Success || string.IsNullOrWhiteSpace(providerIntent.ProviderIntentId))
        {
            var failed = await customerPortalManager.MarkPaymentIntentFailedAsync(intent.Id, providerIntent.ErrorMessage, DateTime.UtcNow).ConfigureAwait(false);
            return MapToDto(failed);
        }

        var processing = await customerPortalManager.MarkPaymentIntentProcessingAsync(intent.Id, providerIntent.ProviderIntentId).ConfigureAwait(false);
        return MapToDto(processing);
    }

    public async Task<CustomerPaymentIntentAppDto?> CompleteMockPaymentAsync(Guid customerId, Guid paymentIntentId, bool success)
    {
        var intent = await customerPortalManager.GetPaymentIntentByIdAsync(paymentIntentId).ConfigureAwait(false);
        if (intent is null || intent.CustomerId != customerId || string.IsNullOrWhiteSpace(intent.ProviderIntentId))
            return null;

        if (intent.Status is not CustomerPaymentIntentStatus.Processing)
            return MapToDto(intent);

        var providerResult = await paymentProvider.CompleteMockAsync(intent.ProviderIntentId, success).ConfigureAwait(false);
        if (!providerResult.Success)
        {
            var failed = await customerPortalManager.MarkPaymentIntentFailedAsync(intent.Id, providerResult.ErrorMessage, DateTime.UtcNow).ConfigureAwait(false);
            return MapToDto(failed);
        }

        var completed = await customerPortalManager.MarkPaymentIntentSucceededPendingReconciliationAsync(intent.Id, DateTime.UtcNow).ConfigureAwait(false);
        return MapToDto(completed);
    }

    private static CustomerPaymentIntentAppDto MapToDto(CustomerPaymentIntentDto intent)
        => new()
        {
            Id = intent.Id,
            CustomerDebtId = intent.CustomerDebtId,
            Amount = intent.Amount,
            Provider = intent.Provider,
            ProviderIntentId = intent.ProviderIntentId,
            Status = (int)intent.Status,
            FailureReason = intent.FailureReason,
            CreatedOnUtc = intent.CreatedOnUtc,
            CompletedOnUtc = intent.CompletedOnUtc
        };
}
