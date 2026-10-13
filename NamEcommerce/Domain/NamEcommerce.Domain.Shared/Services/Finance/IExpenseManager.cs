using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Domain.Shared.Services.Finance;

public interface IExpenseManager
{
    Task<ExpenseDto?> GetExpenseByIdAsync(Guid id);
    Task<IPagedDataDto<ExpenseDto>> GetExpensesAsync(
        int pageIndex, int pageSize, IList<Guid>? orderIds = null,
        string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null,
        ExpenseType? expenseType = null, ExpenseSortByEnum? sortBy = null);

    Task<CreateExpenseResultDto> CreateExpenseAsync(CreateExpenseDto dto);
    Task UpdateExpenseAsync(UpdateExpenseDto dto);
    Task DeleteExpenseAsync(Guid id);
}
