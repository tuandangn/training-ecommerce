using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface IBankTransferPaymentIntentAppService
{
    Task<BankTransferPaymentIntentAppDto?> GetByIdAsync(Guid id);
    Task<BankTransferPaymentIntentResultAppDto> GetStatusAsync(Guid id);
    Task<BankTransferPaymentIntentResultAppDto> CreateAsync(CreateBankTransferPaymentIntentAppDto dto);
    Task<BankTransferPaymentIntentResultAppDto> ConfirmManuallyAsync(ManualConfirmBankTransferPaymentIntentAppDto dto);
    Task<BankTransferPaymentIntentResultAppDto> ConfirmFromProviderAsync(ProviderConfirmBankTransferPaymentIntentAppDto dto);
    Task<BankTransferProviderProcessingResultAppDto> ProcessProviderTransactionAsync(ProcessBankTransferProviderTransactionAppDto dto);
}
