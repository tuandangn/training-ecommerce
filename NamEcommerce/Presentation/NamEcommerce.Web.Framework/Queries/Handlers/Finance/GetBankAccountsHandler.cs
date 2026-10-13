using MediatR;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Models.Finance;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Finance;

public sealed class GetBankAccountsHandler(IBankAccountAppService bankAccountAppService) : IRequestHandler<GetBankAccountsQuery, IEnumerable<BankAccountModel>>
{
    public async Task<IEnumerable<BankAccountModel>> Handle(GetBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var bankAccounts = await bankAccountAppService.GetBankAccountsAsync(request.IncludeInactive).ConfigureAwait(false);

        return bankAccounts.Select(bankAccount => new BankAccountModel
        {
            Id = bankAccount.Id,
            Code = bankAccount.Code,
            BankCode = bankAccount.BankCode,
            BankName = bankAccount.BankName,
            DisplayName = bankAccount.DisplayName,
            IsActive = bankAccount.IsActive,
            IsDefault = bankAccount.IsDefault,
            AccountNumber = bankAccount.AccountNumber,
            AccountHolderName = bankAccount.AccountHolderName
        }).ToList();
    }
}
