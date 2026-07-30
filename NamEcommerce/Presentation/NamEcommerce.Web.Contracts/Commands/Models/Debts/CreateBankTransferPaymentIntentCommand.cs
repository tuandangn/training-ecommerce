using NamEcommerce.Web.Contracts.Models.FastSales;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

[Serializable]
public sealed class CreateBankTransferPaymentIntentCommand : ICommand<BankTransferPaymentIntentResultModel>
{
    public required decimal Amount { get; init; }
    public Guid CustomerId { get; init; }
    public string? Note { get; init; }
}
