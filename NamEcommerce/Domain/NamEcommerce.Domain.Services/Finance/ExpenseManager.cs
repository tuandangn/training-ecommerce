using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Domain.Services.Finance;

public class ExpenseManager : IExpenseManager
{
    private readonly IRepository<Expense> _expenseRepository;
    private readonly IEntityDataReader<Expense> _expenseDataReader;

    public ExpenseManager(IRepository<Expense> expenseRepository, IEntityDataReader<Expense> expenseDataReader)
    {
        _expenseRepository = expenseRepository;
        _expenseDataReader = expenseDataReader;
    }

    public async Task<CreateExpenseResultDto> CreateExpenseAsync(CreateExpenseDto dto)
    {
        var expense = new Expense(Guid.NewGuid(), dto.Title, dto.Amount, dto.ExpenseType, dto.IncurredDate, dto.RecordedByUserId);
        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            expense.UpdateInfo(dto.Title, dto.Description, dto.Amount, dto.ExpenseType, dto.IncurredDate);
        }
        
        await _expenseRepository.InsertAsync(expense);
        return new CreateExpenseResultDto { CreatedId = expense.Id };
    }

    public async Task UpdateExpenseAsync(UpdateExpenseDto dto)
    {
        var expense = await _expenseDataReader.GetByIdAsync(dto.Id);
        if (expense is null)
            throw new ArgumentException($"Expense with ID {dto.Id} not found.");

        expense.UpdateInfo(dto.Title, dto.Description, dto.Amount, dto.ExpenseType, dto.IncurredDate);
        await _expenseRepository.UpdateAsync(expense);
    }

    public async Task DeleteExpenseAsync(Guid id)
    {
        var expense = await _expenseDataReader.GetByIdAsync(id);
        if (expense is not null)
        {
            await _expenseRepository.DeleteAsync(expense);
        }
    }
}
