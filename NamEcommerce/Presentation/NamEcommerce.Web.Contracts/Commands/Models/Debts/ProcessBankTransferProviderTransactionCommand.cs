using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

[Serializable]
public sealed class ProcessBankTransferProviderTransactionCommand : ICommand<BankTransferPaymentIntentResultModel>
{
    public required string ReferenceCode { get; init; }
    public required decimal Amount { get; init; }
    public required string BankId { get; init; }
    public required string AccountNo { get; init; }
    public required string ProviderTransactionId { get; init; }
    public required int Source { get; init; }
    public string? RawPayload { get; init; }
    public DateTime ConfirmedAtUtc { get; init; } = DateTime.UtcNow;
}
