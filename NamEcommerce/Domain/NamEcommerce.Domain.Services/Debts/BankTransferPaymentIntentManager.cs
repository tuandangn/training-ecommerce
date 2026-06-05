using System.Text;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class BankTransferPaymentIntentManager(
    IRepository<BankTransferPaymentIntent> intentRepository,
    IEntityDataReader<BankTransferPaymentIntent> intentReader) : IBankTransferPaymentIntentManager
{
    public async Task<BankTransferPaymentIntentDto> CreateAsync(CreateBankTransferPaymentIntentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var referenceCode = GenerateReferenceCode(dto.TransferContentPrefix);
        var qrImageUrl = BuildVietQrImageUrl(dto.BankId, dto.AccountNo, dto.Template, dto.Amount, referenceCode, dto.AccountName);
        var intent = new BankTransferPaymentIntent(
            referenceCode,
            dto.Amount,
            dto.CustomerId,
            dto.BankId.Trim(),
            dto.AccountNo.Trim(),
            dto.AccountName.Trim(),
            dto.Template.Trim(),
            qrImageUrl,
            dto.IntentExpiryMinutes,
            dto.Note);

        var inserted = await intentRepository.InsertAsync(intent).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<BankTransferPaymentIntentDto?> GetByIdAsync(Guid id)
    {
        var intent = await intentReader.GetByIdAsync(id).ConfigureAwait(false);
        return intent is null ? null : MapToDto(intent);
    }

    public Task<BankTransferPaymentIntentDto?> GetByReferenceCodeAsync(string referenceCode)
    {
        var normalized = NormalizeReference(referenceCode);
        var intent = intentReader.DataSource.FirstOrDefault(x => x.ReferenceCode == normalized);
        return Task.FromResult(intent is null ? null : MapToDto(intent));
    }

    public async Task<BankTransferPaymentIntentDto> ConfirmManuallyAsync(Guid id, Guid verifiedByUserId, string? note)
    {
        if (verifiedByUserId == Guid.Empty)
            throw new NamEcommerceDomainException("Error.UserRequired");

        var intent = await intentRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.PaymentIntentIsNotFound");

        intent.ConfirmManually(verifiedByUserId, note, DateTime.UtcNow);
        var updated = await intentRepository.UpdateAsync(intent).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task<BankTransferPaymentIntentDto> ConfirmFromProviderAsync(ConfirmBankTransferPaymentIntentDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var intent = await intentRepository.GetByIdAsync(dto.IntentId).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.PaymentIntentIsNotFound");

        if (intent.ReferenceCode != NormalizeReference(dto.ReferenceCode)
            || intent.Amount != dto.Amount
            || intent.BankId != dto.BankId.Trim()
            || intent.AccountNo != dto.AccountNo.Trim())
        {
            throw new NamEcommerceDomainException("Error.PaymentIntentVerificationMismatch");
        }

        var duplicatedProviderTransaction = intentReader.DataSource.Any(x =>
            x.ProviderTransactionId == dto.ProviderTransactionId && x.Id != intent.Id);
        if (duplicatedProviderTransaction)
            throw new NamEcommerceDomainException("Error.PaymentIntentProviderTransactionDuplicated");

        intent.ConfirmFromProvider(dto.ProviderTransactionId, dto.Source, dto.RawPayload, dto.ConfirmedAtUtc);
        var updated = await intentRepository.UpdateAsync(intent).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task<BankTransferPaymentIntentDto> ExpireIfPendingAsync(Guid id, DateTime nowUtc)
    {
        var intent = await intentRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.PaymentIntentIsNotFound");

        if (intent.Status == BankTransferPaymentIntentStatus.Pending && intent.ExpiresAtUtc <= nowUtc)
        {
            intent.Expire(nowUtc);
            var updated = await intentRepository.UpdateAsync(intent).ConfigureAwait(false);
            return MapToDto(updated);
        }

        return MapToDto(intent);
    }

    public async Task<BankTransferPaymentIntentDto> ConsumeAsync(Guid id, Guid orderId, Guid? deliveryNoteId, Guid? customerDebtId, Guid customerPaymentId)
    {
        var intent = await intentRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.PaymentIntentIsNotFound");

        intent.Consume(orderId, deliveryNoteId, customerDebtId, customerPaymentId, DateTime.UtcNow);
        var updated = await intentRepository.UpdateAsync(intent).ConfigureAwait(false);
        return MapToDto(updated);
    }

    private string GenerateReferenceCode(string prefix)
    {
        var safePrefix = NormalizeReference(prefix);
        if (safePrefix.Length > 8)
            safePrefix = safePrefix[..8];

        for (var i = 0; i < 50; i++)
        {
            var candidate = $"{safePrefix}{DateTime.UtcNow:yyMMddHHmmss}{i:D2}";
            if (candidate.Length > 25)
                candidate = candidate[..25];
            if (!intentReader.DataSource.Any(x => x.ReferenceCode == candidate))
                return candidate;
        }

        throw new NamEcommerceDomainException("Error.PaymentIntentReferenceCannotGenerate");
    }

    private static string BuildVietQrImageUrl(string bankId, string accountNo, string template, decimal amount, string referenceCode, string accountName)
    {
        var encodedInfo = Uri.EscapeDataString(referenceCode);
        var encodedName = Uri.EscapeDataString(accountName.Trim());
        var normalizedAmount = decimal.Truncate(amount).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        return $"https://img.vietqr.io/image/{bankId.Trim()}-{accountNo.Trim()}-{template.Trim()}.png?amount={normalizedAmount}&addInfo={encodedInfo}&accountName={encodedName}";
    }

    private static string NormalizeReference(string value)
    {
        var builder = new StringBuilder();
        foreach (var ch in value.Trim().ToUpperInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
                builder.Append(ch);
        }

        return builder.Length == 0 ? "QS" : builder.ToString();
    }

    private static BankTransferPaymentIntentDto MapToDto(BankTransferPaymentIntent intent)
        => new(intent.Id)
        {
            ReferenceCode = intent.ReferenceCode,
            Amount = intent.Amount,
            CustomerId = intent.CustomerId,
            BankId = intent.BankId,
            AccountNo = intent.AccountNo,
            AccountName = intent.AccountName,
            Template = intent.Template,
            QrImageUrl = intent.QrImageUrl,
            Status = intent.Status,
            Note = intent.Note,
            OrderId = intent.OrderId,
            DeliveryNoteId = intent.DeliveryNoteId,
            CustomerDebtId = intent.CustomerDebtId,
            CustomerPaymentId = intent.CustomerPaymentId,
            VerificationSource = intent.VerificationSource,
            ProviderTransactionId = intent.ProviderTransactionId,
            RawPayload = intent.RawPayload,
            VerifiedAtUtc = intent.VerifiedAtUtc,
            VerifiedByUserId = intent.VerifiedByUserId,
            ExpiresAtUtc = intent.ExpiresAtUtc,
            ExpiredAtUtc = intent.ExpiredAtUtc,
            CreatedOnUtc = intent.CreatedOnUtc,
            UpdatedOnUtc = intent.UpdatedOnUtc
        };
}
