using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Application.Services.Debts;

public sealed class BankTransferReceivingAccountResolver(
    IBankAccountManager bankAccountManager,
    BankTransferPaymentSettings settings) : IBankTransferReceivingAccountResolver
{
    public async Task<BankTransferReceivingAccountAppDto?> ResolveAsync()
    {
        var defaultAccount = await bankAccountManager.GetDefaultAsync().ConfigureAwait(false);
        if (defaultAccount is not null)
        {
            var receivingAccount = ToReceivingAccount(defaultAccount);
            if (receivingAccount.IsConfigured)
                return receivingAccount;
        }

        var accounts = await bankAccountManager.GetAllAsync(includeInactive: true).ConfigureAwait(false);
        if (accounts.Count == 0 && HasConfiguredSettings())
        {
            var result = await bankAccountManager.CreateAsync(new CreateBankAccountDto
            {
                DisplayName = $"{settings.BankId} - {settings.AccountNo}",
                BankCode = settings.BankId,
                BankName = settings.BankId,
                AccountNumber = settings.AccountNo,
                AccountHolderName = settings.AccountName,
                OpeningBalance = 0,
                SetAsDefault = true
            }).ConfigureAwait(false);

            var createdAccount = await bankAccountManager.GetByIdAsync(result.CreatedId).ConfigureAwait(false);
            if (createdAccount is not null)
                return ToReceivingAccount(createdAccount);
        }

        return HasConfiguredSettings()
            ? new BankTransferReceivingAccountAppDto
            {
                BankId = settings.BankId,
                AccountNo = settings.AccountNo,
                AccountName = settings.AccountName
            }
            : null;
    }

    private bool HasConfiguredSettings()
        => !string.IsNullOrWhiteSpace(settings.BankId)
            && !string.IsNullOrWhiteSpace(settings.AccountNo)
            && !string.IsNullOrWhiteSpace(settings.AccountName);

    private static BankTransferReceivingAccountAppDto ToReceivingAccount(BankAccountDto account)
        => new()
        {
            BankId = account.BankCode,
            AccountNo = account.AccountNumber,
            AccountName = account.AccountHolderName
        };
}
