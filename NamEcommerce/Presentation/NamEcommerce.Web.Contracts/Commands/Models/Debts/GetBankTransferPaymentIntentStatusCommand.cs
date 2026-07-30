using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

[Serializable]
public sealed class GetBankTransferPaymentIntentStatusCommand : ICommand<BankTransferPaymentIntentResultModel>
{
    public required Guid IntentId { get; init; }
}
