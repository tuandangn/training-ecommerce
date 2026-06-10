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
        var existingId = budgetReader.DataSource
            .Where(b => b.ExpenseType == dto.ExpenseType && b.Year == dto.Year && b.Month == dto.Month)
            .Select(b => b.Id)
            .FirstOrDefault();

        if (existingId != Guid.Empty)
        {
            var existing = await budgetRepository.GetByIdAsync(existingId).ConfigureAwait(false);
            if (existing is not null)
            {
                existing.UpdateAmount(dto.Amount);
                await budgetRepository.UpdateAsync(existing).ConfigureAwait(false);
                return;
            }
        }

        var budget = new ExpenseBudget(Guid.NewGuid(), dto.ExpenseType, dto.Year, dto.Month, dto.Amount);
        await budgetRepository.InsertAsync(budget).ConfigureAwait(false);
    }
}
