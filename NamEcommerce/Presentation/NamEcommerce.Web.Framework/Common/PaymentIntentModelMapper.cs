using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Framework.Common;

internal static class PaymentIntentModelMapper
{
    public static BankTransferPaymentIntentResultModel MapIntentResult(BankTransferPaymentIntentResultAppDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            Intent = result.Intent is null
                ? null
                : MapIntent(result.Intent)
        };

    public static BankTransferPaymentIntentResultModel MapIntentResult(BankTransferProviderProcessingResultAppDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            VerificationLogId = result.VerificationLogId,
            Intent = result.Intent is null
                ? null
                : MapIntent(result.Intent)
        };

    private static BankTransferPaymentIntentModel MapIntent(BankTransferPaymentIntentAppDto intent)
    {
        var status = (BankTransferPaymentIntentStatus)intent.Status;
        return new()
        {
            Id = intent.Id,
            ReferenceCode = intent.ReferenceCode,
            Amount = intent.Amount,
            BankId = intent.BankId,
            AccountNo = intent.AccountNo,
            AccountName = intent.AccountName,
            QrImageUrl = intent.QrImageUrl,
            Status = intent.Status,
            ExpiresAtUtc = intent.ExpiresAtUtc,
            VerificationSource = intent.VerificationSource,
            VerifiedAtUtc = intent.VerifiedAtUtc,
            IsPending = status is BankTransferPaymentIntentStatus.Pending,
            IsCancelled = status is BankTransferPaymentIntentStatus.Cancelled,
            IsExpired = status is BankTransferPaymentIntentStatus.Expired,
            IsConfirmed = status is BankTransferPaymentIntentStatus.Confirmed or BankTransferPaymentIntentStatus.ManuallyConfirmed
        };
    }
}
