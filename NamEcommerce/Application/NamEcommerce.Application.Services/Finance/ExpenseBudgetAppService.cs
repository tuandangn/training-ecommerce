using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Application.Services.Finance;

public class ExpenseBudgetAppService(
    IExpenseBudgetManager budgetManager,
    IEntityDataReader<ExpenseBudget> budgetReader) : IExpenseBudgetAppService
{
    public Task<IReadOnlyCollection<ExpenseBudgetAppDto>> GetBudgetsForMonthAsync(int year, int month)
    {
        var result = budgetReader.DataSource
            .Where(b => b.Year == year && b.Month == month)
            .Select(b => new ExpenseBudgetAppDto
            {
                ExpenseType = (int)b.ExpenseType,
                Year = b.Year,
                Month = b.Month,
                Amount = b.Amount
            })
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ExpenseBudgetAppDto>>(result);
    }

    public async Task<UpsertExpenseBudgetResultAppDto> UpsertBudgetAsync(UpsertExpenseBudgetAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new UpsertExpenseBudgetResultAppDto { Success = false, ErrorMessage = errorMessage };

        await budgetManager.UpsertAsync(new UpsertExpenseBudgetDto
        {
            ExpenseType = (ExpenseType)dto.ExpenseType,
            Year = dto.Year,
            Month = dto.Month,
            Amount = dto.Amount
        }).ConfigureAwait(false);

        return new UpsertExpenseBudgetResultAppDto { Success = true };
    }
}
