using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICassoBankTransferAppService
{
    Task<CassoBankTransferProcessingResultAppDto> ProcessWebhookAsync(ProcessCassoWebhookAppDto dto);
    Task<CassoBankTransferProcessingResultAppDto> RunReconciliationAsync(RunCassoReconciliationAppDto dto);
}
