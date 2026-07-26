using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Models.FastSales;

namespace NamEcommerce.Web.Services.FastSales;

public sealed class FastSaleModelFactory(
    IBankTransferReceivingAccountResolver receivingAccountResolver,
    BankTransferPaymentSettings bankTransferPaymentSettings) : IFastSaleModelFactory
{
    public async Task<OrderQuickCreateModel> PrepareFastSaleModelAsync()
    {
        var receivingAccount = await receivingAccountResolver.ResolveAsync().ConfigureAwait(false);

        return new OrderQuickCreateModel
        {
            BankTransferEnabled = bankTransferPaymentSettings.Enabled && receivingAccount?.IsConfigured == true,
            BankAccountLabel = string.IsNullOrWhiteSpace(receivingAccount?.AccountNo)
                ? string.Empty
                : $"{receivingAccount.BankId} {receivingAccount.AccountNo} - {receivingAccount.AccountName}",
            ManualBankTransferConfirmEnabled = bankTransferPaymentSettings.Verification.AllowManualConfirm
        };
    }
}
