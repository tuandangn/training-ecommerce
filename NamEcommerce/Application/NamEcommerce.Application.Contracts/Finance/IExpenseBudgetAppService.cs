using NamEcommerce.Application.Contracts.Dtos.Finance;

namespace NamEcommerce.Application.Contracts.Finance;

public interface IExpenseBudgetAppService
{
    Task<IReadOnlyCollection<ExpenseBudgetAppDto>> GetBudgetsForMonthAsync(int year, int month);
    Task<UpsertExpenseBudgetResultAppDto> UpsertBudgetAsync(UpsertExpenseBudgetAppDto dto);
}
