using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Domain.Services.Finance;

public class ExpenseBudgetManager(
    IRepository<ExpenseBudget> budgetRepository,
    IEntityDataReader<ExpenseBudget> budgetReader) : IExpenseBudgetManager
{
    public async Task UpsertAsync(UpsertExpenseBudgetDto dto)
    {
        var existing = budgetReader.DataSource
            .FirstOrDefault(b => b.ExpenseType == dto.ExpenseType
                && b.Year == dto.Year
                && b.Month == dto.Month);

        if (existing is not null)
        {
            existing.UpdateAmount(dto.Amount);
            await budgetRepository.UpdateAsync(existing).ConfigureAwait(false);
        }
        else
        {
            var budget = new ExpenseBudget(Guid.NewGuid(), dto.ExpenseType, dto.Year, dto.Month, dto.Amount);
            await budgetRepository.InsertAsync(budget).ConfigureAwait(false);
        }
    }
}
