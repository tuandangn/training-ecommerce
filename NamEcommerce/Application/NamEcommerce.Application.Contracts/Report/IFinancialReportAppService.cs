using NamEcommerce.Application.Contracts.Dtos.Report;

namespace NamEcommerce.Application.Contracts.Report;

public interface IFinancialReportAppService
{
    Task<ProfitLossSummaryAppDto> GetProfitLossSummaryAsync(DateTime? fromDate, DateTime? toDate);
    Task<(decimal TotalCost, bool HasPendingCost)> GetNetCogsForDeliveryNotesAsync(IEnumerable<Guid> deliveryNoteIds);
}
