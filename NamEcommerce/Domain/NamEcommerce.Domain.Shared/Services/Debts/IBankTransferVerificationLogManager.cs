using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface IBankTransferVerificationLogManager
{
    Task<BankTransferVerificationLogDto> CreateReceivedAsync(CreateBankTransferVerificationLogDto dto);
    Task<BankTransferVerificationLogDto> MarkMatchedAsync(Guid id, Guid paymentIntentId);
    Task<BankTransferVerificationLogDto> MarkRejectedAsync(Guid id, string errorMessage);
    Task<BankTransferVerificationLogDto> MarkDuplicateAsync(Guid id, Guid? paymentIntentId, string errorMessage);
}
