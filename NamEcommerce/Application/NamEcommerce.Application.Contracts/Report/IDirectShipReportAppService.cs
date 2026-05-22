using NamEcommerce.Application.Contracts.Dtos.Report;

namespace NamEcommerce.Application.Contracts.Report;

public interface IDirectShipReportAppService
{
    Task<DirectShipReportAppDto> GetReportAsync(DateTime? fromDate, DateTime? toDate);
}
