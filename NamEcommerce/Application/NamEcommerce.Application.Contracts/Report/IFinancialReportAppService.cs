using NamEcommerce.Application.Contracts.Dtos.Report;

namespace NamEcommerce.Application.Contracts.Report;

public interface IFinancialReportAppService
{
    Task<ProfitLossSummaryAppDto> GetProfitLossSummaryAsync(DateTime? fromDate, DateTime? toDate);
}
