using System.Globalization;
using System.Text;
using System.Text.Json;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CassoTransactionMapper(BankTransferPaymentSettings settings)
{
    public CassoMappedTransactionAppDto Map(
        CassoTransactionAppDto transaction,
        BankTransferVerificationSource source)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        if (transaction.Amount <= 0)
            return Ignore("Ignored.NonIncomingTransfer");

        var providerTransactionId = transaction.Id?.ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(providerTransactionId))
            return Ignore("Ignored.ProviderTransactionIdMissing");

        var accountNo = FirstNonEmpty(transaction.BankSubAccId, transaction.SubAccId, transaction.BankSubAccIdCamel);
        if (string.IsNullOrWhiteSpace(accountNo))
            return Ignore("Ignored.AccountNoMissing");

        var referenceCode = ExtractReferenceCode(transaction.Description);
        if (string.IsNullOrWhiteSpace(referenceCode))
            return Ignore("Ignored.ReferenceCodeMissing");

        var confirmedAtUtc = ParseConfirmedAtUtc(transaction.When);
        var bankId = FirstNonEmpty(transaction.BankAbbreviation, transaction.BankCodeName, settings.BankId);
        if (string.IsNullOrWhiteSpace(bankId))
            return Ignore("Ignored.BankIdMissing");

        return new CassoMappedTransactionAppDto
        {
            CanProcess = true,
            ProviderTransaction = new ProcessBankTransferProviderTransactionAppDto
            {
                ReferenceCode = referenceCode,
                Amount = transaction.Amount,
                BankId = bankId,
                AccountNo = accountNo,
                ProviderTransactionId = providerTransactionId,
                Source = (int)source,
                RawPayload = JsonSerializer.Serialize(transaction),
                ConfirmedAtUtc = confirmedAtUtc
            }
        };
    }

    private string? ExtractReferenceCode(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return null;

        var prefix = string.IsNullOrWhiteSpace(settings.TransferContentPrefix)
            ? "QS"
            : settings.TransferContentPrefix.Trim().ToUpperInvariant();

        var tokens = NormalizeTokens(description);
        return tokens.FirstOrDefault(token => token.StartsWith(prefix, StringComparison.Ordinal) && token.Length <= 25);
    }

    private static IList<string> NormalizeTokens(string description)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (var ch in description.ToUpperInvariant())
        {
            if (ch is >= 'A' and <= 'Z' or >= '0' and <= '9')
            {
                current.Append(ch);
                continue;
            }

            AddToken(tokens, current);
        }

        AddToken(tokens, current);
        return tokens;
    }

    private static void AddToken(ICollection<string> tokens, StringBuilder current)
    {
        if (current.Length == 0)
            return;

        tokens.Add(current.ToString());
        current.Clear();
    }

    private static DateTime ParseConfirmedAtUtc(string? value)
    {
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            return parsed.ToUniversalTime();

        return DateTime.UtcNow;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static CassoMappedTransactionAppDto Ignore(string reason)
        => new()
        {
            CanProcess = false,
            IgnoreReason = reason
        };
}
