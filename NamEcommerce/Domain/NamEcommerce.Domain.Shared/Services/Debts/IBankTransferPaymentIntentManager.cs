using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface IBankTransferPaymentIntentManager
{
    Task<BankTransferPaymentIntentDto> CreateAsync(CreateBankTransferPaymentIntentDto dto);
    Task<BankTransferPaymentIntentDto?> GetByIdAsync(Guid id);
    Task<BankTransferPaymentIntentDto?> GetByReferenceCodeAsync(string referenceCode);
    Task<BankTransferPaymentIntentDto> ConfirmManuallyAsync(Guid id, Guid verifiedByUserId, string? note);
    Task<BankTransferPaymentIntentDto> ConfirmFromProviderAsync(ConfirmBankTransferPaymentIntentDto dto);
    Task<BankTransferPaymentIntentDto> ConsumeAsync(Guid id, Guid orderId, Guid deliveryNoteId, Guid customerDebtId, Guid customerPaymentId);
}
