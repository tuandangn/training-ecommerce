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
    IBankTransferVerificationLogManager verificationLogManager,
    BankTransferPaymentSettings settings,
    ICurrentUserAccessor currentUserAccessor) : IBankTransferPaymentIntentAppService
{
    public async Task<BankTransferPaymentIntentAppDto?> GetByIdAsync(Guid id)
    {
        var intent = await paymentIntentManager.GetByIdAsync(id).ConfigureAwait(false);
        return intent is null ? null : MapToDto(intent);
    }

    public async Task<BankTransferPaymentIntentResultAppDto> GetStatusAsync(Guid id)
    {
        try
        {
            var intent = await paymentIntentManager.ExpireIfPendingAsync(id, DateTime.UtcNow).ConfigureAwait(false);
            return BankTransferPaymentIntentResultAppDto.CreateSuccess(MapToDto(intent));
        }
        catch (Exception ex)
        {
            return BankTransferPaymentIntentResultAppDto.CreateError(ex.Message);
        }
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

    public async Task<BankTransferProviderProcessingResultAppDto> ProcessProviderTransactionAsync(ProcessBankTransferProviderTransactionAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.ReferenceCode))
            return CreateProviderProcessingError("Error.PaymentIntentReferenceRequired");
        if (dto.Amount <= 0)
            return CreateProviderProcessingError("Error.PaymentAmountMustBePositive");
        if (string.IsNullOrWhiteSpace(dto.BankId))
            return CreateProviderProcessingError("Error.BankIdRequired");
        if (string.IsNullOrWhiteSpace(dto.AccountNo))
            return CreateProviderProcessingError("Error.BankAccountNoRequired");
        if (string.IsNullOrWhiteSpace(dto.ProviderTransactionId))
            return CreateProviderProcessingError("Error.ProviderTransactionIdRequired");
        if (!Enum.IsDefined(typeof(BankTransferVerificationSource), dto.Source))
            return CreateProviderProcessingError("Error.BankTransferVerificationSourceInvalid");

        var source = (BankTransferVerificationSource)dto.Source;
        if (source == BankTransferVerificationSource.Manual)
            return CreateProviderProcessingError("Error.PaymentIntentProviderSourceInvalid");

        BankTransferVerificationLogDto? verificationLog = null;
        BankTransferPaymentIntentDto? existingIntent = null;

        try
        {
            verificationLog = await verificationLogManager.CreateReceivedAsync(new CreateBankTransferVerificationLogDto
            {
                ReferenceCode = dto.ReferenceCode,
                Amount = dto.Amount,
                BankId = dto.BankId,
                AccountNo = dto.AccountNo,
                ProviderTransactionId = dto.ProviderTransactionId,
                Source = source,
                RawPayload = dto.RawPayload,
                ProviderConfirmedAtUtc = dto.ConfirmedAtUtc
            }).ConfigureAwait(false);

            existingIntent = await paymentIntentManager.GetByReferenceCodeAsync(dto.ReferenceCode).ConfigureAwait(false);
            if (existingIntent is null)
            {
                await verificationLogManager
                    .MarkRejectedAsync(verificationLog.Id, "Error.PaymentIntentIsNotFound")
                    .ConfigureAwait(false);

                return CreateProviderProcessingError("Error.PaymentIntentIsNotFound", verificationLog.Id);
            }

            var intent = await paymentIntentManager.ConfirmFromProviderAsync(new ConfirmBankTransferPaymentIntentDto
            {
                IntentId = existingIntent.Id,
                ReferenceCode = dto.ReferenceCode,
                Amount = dto.Amount,
                BankId = dto.BankId,
                AccountNo = dto.AccountNo,
                ProviderTransactionId = dto.ProviderTransactionId,
                Source = source,
                RawPayload = dto.RawPayload,
                ConfirmedAtUtc = dto.ConfirmedAtUtc
            }).ConfigureAwait(false);

            await verificationLogManager.MarkMatchedAsync(verificationLog.Id, intent.Id).ConfigureAwait(false);

            return new BankTransferProviderProcessingResultAppDto
            {
                Success = true,
                Intent = MapToDto(intent),
                VerificationLogId = verificationLog.Id
            };
        }
        catch (Exception ex)
        {
            if (verificationLog is null)
                return CreateProviderProcessingError(ex.Message);

            var originalError = ex.Message;
            try
            {
                if (originalError == "Error.PaymentIntentProviderTransactionDuplicated")
                {
                    await verificationLogManager
                        .MarkDuplicateAsync(verificationLog.Id, existingIntent?.Id, originalError)
                        .ConfigureAwait(false);
                }
                else
                {
                    await verificationLogManager
                        .MarkRejectedAsync(verificationLog.Id, originalError)
                        .ConfigureAwait(false);
                }
            }
            catch
            {
                return CreateProviderProcessingError(originalError, verificationLog.Id);
            }

            return CreateProviderProcessingError(originalError, verificationLog.Id);
        }
    }

    private static BankTransferProviderProcessingResultAppDto CreateProviderProcessingError(
        string? errorMessage,
        Guid? verificationLogId = null)
        => new()
        {
            Success = false,
            ErrorMessage = errorMessage,
            VerificationLogId = verificationLogId
        };

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
