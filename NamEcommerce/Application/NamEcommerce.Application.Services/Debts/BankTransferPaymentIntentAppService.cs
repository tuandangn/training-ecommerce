using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Debts;

public sealed class BankTransferPaymentIntentAppService(
    IBankTransferPaymentIntentManager paymentIntentManager,
    BankTransferPaymentSettings settings,
    ICurrentUserAccessor currentUserAccessor) : IBankTransferPaymentIntentAppService
{
    public async Task<BankTransferPaymentIntentAppDto?> GetByIdAsync(Guid id)
    {
        var intent = await paymentIntentManager.GetByIdAsync(id).ConfigureAwait(false);
        return intent is null ? null : MapToDto(intent);
    }

    public async Task<BankTransferPaymentIntentResultAppDto> CreateAsync(CreateBankTransferPaymentIntentAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return BankTransferPaymentIntentResultAppDto.CreateError(errorMessage);

        if (!settings.Enabled)
            return BankTransferPaymentIntentResultAppDto.CreateError("Error.BankTransferPaymentDisabled");
        if (string.IsNullOrWhiteSpace(settings.BankId)
            || string.IsNullOrWhiteSpace(settings.AccountNo)
            || string.IsNullOrWhiteSpace(settings.AccountName))
        {
            return BankTransferPaymentIntentResultAppDto.CreateError("Error.BankTransferAccountNotConfigured");
        }

        try
        {
            var intent = await paymentIntentManager.CreateAsync(new CreateBankTransferPaymentIntentDto
            {
                Amount = dto.Amount,
                CustomerId = dto.CustomerId,
                Note = dto.Note,
                BankId = settings.BankId,
                AccountNo = settings.AccountNo,
                AccountName = settings.AccountName,
                Template = string.IsNullOrWhiteSpace(settings.Template) ? "compact2" : settings.Template,
                TransferContentPrefix = string.IsNullOrWhiteSpace(settings.TransferContentPrefix)
                    ? "QS"
                    : settings.TransferContentPrefix,
                IntentExpiryMinutes = settings.IntentExpiryMinutes
            }).ConfigureAwait(false);

            return BankTransferPaymentIntentResultAppDto.CreateSuccess(MapToDto(intent));
        }
        catch (Exception ex)
        {
            return BankTransferPaymentIntentResultAppDto.CreateError(ex.Message);
        }
    }

    public async Task<BankTransferPaymentIntentResultAppDto> ConfirmManuallyAsync(ManualConfirmBankTransferPaymentIntentAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!settings.Verification.AllowManualConfirm)
            return BankTransferPaymentIntentResultAppDto.CreateError("Error.BankTransferManualConfirmDisabled");

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser is null)
            return BankTransferPaymentIntentResultAppDto.CreateError("Error.UserRequired");

        try
        {
            var intent = await paymentIntentManager
                .ConfirmManuallyAsync(dto.IntentId, currentUser.Id, dto.Note)
                .ConfigureAwait(false);

            return BankTransferPaymentIntentResultAppDto.CreateSuccess(MapToDto(intent));
        }
        catch (Exception ex)
        {
            return BankTransferPaymentIntentResultAppDto.CreateError(ex.Message);
        }
    }

    public async Task<BankTransferPaymentIntentResultAppDto> ConfirmFromProviderAsync(ProviderConfirmBankTransferPaymentIntentAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.ReferenceCode))
            return BankTransferPaymentIntentResultAppDto.CreateError("Error.PaymentIntentReferenceRequired");
        if (dto.Amount <= 0)
            return BankTransferPaymentIntentResultAppDto.CreateError("Error.PaymentAmountMustBePositive");
        if (string.IsNullOrWhiteSpace(dto.ProviderTransactionId))
            return BankTransferPaymentIntentResultAppDto.CreateError("Error.ProviderTransactionIdRequired");

        try
        {
            var existingIntent = await paymentIntentManager.GetByReferenceCodeAsync(dto.ReferenceCode).ConfigureAwait(false);
            if (existingIntent is null)
                return BankTransferPaymentIntentResultAppDto.CreateError("Error.PaymentIntentIsNotFound");

            var intent = await paymentIntentManager.ConfirmFromProviderAsync(new ConfirmBankTransferPaymentIntentDto
            {
                IntentId = existingIntent.Id,
                ReferenceCode = dto.ReferenceCode,
                Amount = dto.Amount,
                BankId = dto.BankId,
                AccountNo = dto.AccountNo,
                ProviderTransactionId = dto.ProviderTransactionId,
                Source = (BankTransferVerificationSource)dto.Source,
                RawPayload = dto.RawPayload,
                ConfirmedAtUtc = dto.ConfirmedAtUtc
            }).ConfigureAwait(false);

            return BankTransferPaymentIntentResultAppDto.CreateSuccess(MapToDto(intent));
        }
        catch (Exception ex)
        {
            return BankTransferPaymentIntentResultAppDto.CreateError(ex.Message);
        }
    }

    private static BankTransferPaymentIntentAppDto MapToDto(BankTransferPaymentIntentDto intent)
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
            Status = (int)intent.Status,
            Note = intent.Note,
            OrderId = intent.OrderId,
            DeliveryNoteId = intent.DeliveryNoteId,
            CustomerDebtId = intent.CustomerDebtId,
            CustomerPaymentId = intent.CustomerPaymentId,
            VerificationSource = intent.VerificationSource.HasValue ? (int)intent.VerificationSource.Value : null,
            ProviderTransactionId = intent.ProviderTransactionId,
            VerifiedAtUtc = intent.VerifiedAtUtc,
            VerifiedByUserId = intent.VerifiedByUserId,
            ExpiresAtUtc = intent.ExpiresAtUtc,
            ExpiredAtUtc = intent.ExpiredAtUtc,
            CreatedOnUtc = intent.CreatedOnUtc,
            UpdatedOnUtc = intent.UpdatedOnUtc
        };
}
