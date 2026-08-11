using NamEcommerce.Web.Contracts.Models.Finance;

namespace NamEcommerce.Web.Contracts.Queries.Models.Finance;

[Serializable]
public sealed record GetBankAccountsQuery() : IRequest<IEnumerable<BankAccountModel>>
{
    public bool IncludeInactive { get; set; }
}
