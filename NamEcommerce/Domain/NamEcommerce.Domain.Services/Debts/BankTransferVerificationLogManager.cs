using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class BankTransferVerificationLogManager(
    IRepository<BankTransferVerificationLog> logRepository) : IBankTransferVerificationLogManager
{
    public async Task<BankTransferVerificationLogDto> CreateReceivedAsync(CreateBankTransferVerificationLogDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var log = new BankTransferVerificationLog(
            dto.ReferenceCode.Trim(),
            dto.Amount,
            dto.BankId.Trim(),
            dto.AccountNo.Trim(),
            dto.ProviderTransactionId.Trim(),
            dto.Source,
            dto.RawPayload,
            dto.ProviderConfirmedAtUtc);

        var inserted = await logRepository.InsertAsync(log).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<BankTransferVerificationLogDto> MarkMatchedAsync(Guid id, Guid paymentIntentId)
    {
        var log = await GetLogAsync(id).ConfigureAwait(false);
        log.MarkMatched(paymentIntentId, DateTime.UtcNow);
        var updated = await logRepository.UpdateAsync(log).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task<BankTransferVerificationLogDto> MarkRejectedAsync(Guid id, string errorMessage)
    {
        var log = await GetLogAsync(id).ConfigureAwait(false);
        log.MarkRejected(errorMessage, DateTime.UtcNow);
        var updated = await logRepository.UpdateAsync(log).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task<BankTransferVerificationLogDto> MarkDuplicateAsync(Guid id, Guid? paymentIntentId, string errorMessage)
    {
        var log = await GetLogAsync(id).ConfigureAwait(false);
        log.MarkDuplicate(paymentIntentId, errorMessage, DateTime.UtcNow);
        var updated = await logRepository.UpdateAsync(log).ConfigureAwait(false);
        return MapToDto(updated);
    }

    private async Task<BankTransferVerificationLog> GetLogAsync(Guid id)
        => await logRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.BankTransferVerificationLogIsNotFound");

    private static BankTransferVerificationLogDto MapToDto(BankTransferVerificationLog log)
        => new(log.Id)
        {
            ReferenceCode = log.ReferenceCode,
            Amount = log.Amount,
            BankId = log.BankId,
            AccountNo = log.AccountNo,
            ProviderTransactionId = log.ProviderTransactionId,
            Source = log.Source,
            Status = log.Status,
            PaymentIntentId = log.PaymentIntentId,
            ErrorMessage = log.ErrorMessage,
            RawPayload = log.RawPayload,
            ProviderConfirmedAtUtc = log.ProviderConfirmedAtUtc,
            CreatedOnUtc = log.CreatedOnUtc,
            UpdatedOnUtc = log.UpdatedOnUtc
        };
}
