using NamEcommerce.Application.Contracts.Dtos.Finance;

namespace NamEcommerce.Application.Contracts.Finance;

public interface IAccountingReportService
{
    Task<IncomeStatementDto> GetIncomeStatementAsync(AccountingPeriod period);
    Task<CashFlowStatementDto> GetCashFlowStatementAsync(AccountingPeriod period);
    Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf, bool includePriorPeriod = true);
}
