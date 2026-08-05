using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Finance;

namespace NamEcommerce.Application.Contracts.Finance;

public interface IExpenseAppService
{
    Task<IPagedDataAppDto<ExpenseAppDto>> GetExpensesAsync(
        int pageIndex = 0, int pageSize = int.MaxValue, IList<Guid>? orderIds = null,
        string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null,
        int? expenseType = null, string? sortBy = null, bool sortDesc = true);
    Task<IEnumerable<ExpenseSummaryAppDto>> GetExpenseSummaryAsync(DateTime? fromDate, DateTime? toDate);
    Task<ExpenseAppDto?> GetExpenseByIdAsync(Guid id);
    Task<CreateExpenseResultAppDto> CreateExpenseAsync(CreateExpenseAppDto dto);
    Task<UpdateExpenseResultAppDto> UpdateExpenseAsync(UpdateExpenseAppDto dto);
    Task<DeleteExpenseResultAppDto> DeleteExpenseAsync(Guid id);
}
