using NamEcommerce.Application.Contracts.Dtos.Finance;

namespace NamEcommerce.Application.Contracts.Finance;

public interface ICashBookService
{
    Task<decimal> GetCashBalanceAsync(DateTime asOf);
    Task<IReadOnlyList<BankBalanceLineDto>> GetBankBalancesAsync(DateTime asOf);
    Task<decimal> GetTotalCashAsync(DateTime asOf);
    Task<CashBookDto> GetCashBookAsync(AccountingPeriod period, Guid? bankAccountId = null);
}
