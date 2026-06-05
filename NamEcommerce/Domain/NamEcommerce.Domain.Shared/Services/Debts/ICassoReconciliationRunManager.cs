using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface ICassoReconciliationRunManager
{
    Task<CassoReconciliationRunDto> StartAsync(StartCassoReconciliationRunDto dto);
    Task<CassoReconciliationRunDto> CompleteAsync(CompleteCassoReconciliationRunDto dto);
    Task<CassoReconciliationRunDto> FailAsync(FailCassoReconciliationRunDto dto);
}
