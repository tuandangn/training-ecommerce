using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Application.Services.Extensions;

namespace NamEcommerce.Application.Services.Finance;

public class ExpenseAppService(IExpenseManager expenseManager,
    IEntityDataReader<Expense> expenseDataReader) : IExpenseAppService
{
    public async Task<IPagedDataAppDto<ExpenseAppDto>> GetExpensesAsync(
        int pageIndex = 0, int pageSize = int.MaxValue, IList<Guid>? orderIds = null,
        string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null,
        int? expenseType = null, string? sortBy = null, bool sortDesc = true)
    {
        var expenseSortBy = sortBy switch
        {
            "amount" => sortDesc ? ExpenseSortByEnum.AmountDesc : ExpenseSortByEnum.Amount,
            "title" => sortDesc ? ExpenseSortByEnum.TitleDesc : ExpenseSortByEnum.Title,
            "type" => ExpenseSortByEnum.ExpenseType,
            _ => sortDesc ? ExpenseSortByEnum.IncurredDateDesc : ExpenseSortByEnum.IncurredDate
        };
        var pagedData = await expenseManager.GetExpensesAsync(pageIndex, pageSize,
            orderIds, keywords, fromDate, toDate,
            (ExpenseType?)expenseType, expenseSortBy).ConfigureAwait(false);

        return pagedData.ToPagedDataAppDto(expense => new ExpenseAppDto
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            AmountWithoutTax = expense.AmountWithoutTax,
            TaxRate = expense.TaxRate,
            Amount = expense.Amount,
            ExpenseType = (int)expense.ExpenseType,
            IncurredDateUtc = expense.IncurredDate,
            SourceOrderId = expense.SourceOrderId,
            SourceCustomerReturnId = expense.SourceCustomerReturnId,
            SourceVendorReturnId = expense.SourceVendorReturnId,
            IsSystemGenerated = expense.IsSystemGenerated
        });
    }

    public async Task<ExpenseAppDto?> GetExpenseByIdAsync(Guid id)
    {
        var expense = await expenseManager.GetExpenseByIdAsync(id).ConfigureAwait(false);

        if (expense is null) 
            return null;

        return new()
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            AmountWithoutTax = expense.AmountWithoutTax,
            TaxRate = expense.TaxRate,
            Amount = expense.Amount,
            ExpenseType = (int)expense.ExpenseType,
            IncurredDateUtc = expense.IncurredDate,
            SourceOrderId = expense.SourceOrderId,
            SourceCustomerReturnId = expense.SourceCustomerReturnId,
            SourceVendorReturnId = expense.SourceVendorReturnId,
            IsSystemGenerated = expense.IsSystemGenerated
        };
    }

    public async Task<CreateExpenseResultAppDto> CreateExpenseAsync(CreateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var result = await expenseManager.CreateExpenseAsync(new CreateExpenseDto
        {
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            ExpenseType = (ExpenseType)dto.ExpenseType,
            IncurredDateUtc = dto.IncurredDateUtc,
            RecordedByUserId = dto.RecordedByUserId,
            OrderId = dto.OrderId,
        }).ConfigureAwait(false);

        return new CreateExpenseResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<UpdateExpenseResultAppDto> UpdateExpenseAsync(UpdateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) 
            return new UpdateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var expense = await expenseDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (expense is null) 
            return new UpdateExpenseResultAppDto { Success = false, ErrorMessage = "Error.ExpenseIsNotFound" };

        await expenseManager.UpdateExpenseAsync(new UpdateExpenseDto
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            AmountWithoutTax = dto.AmountWithoutTax,
            TaxRate = dto.TaxRate,
            IncurredDateUtc = dto.IncurredDateUtc,
            ExpenseType = (ExpenseType) dto.ExpenseType
        }).ConfigureAwait(false);

        return new UpdateExpenseResultAppDto { Success = true, UpdatedId = dto.Id };
    }

    public async Task<DeleteExpenseResultAppDto> DeleteExpenseAsync(Guid id)
    {
        var expense = await expenseDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (expense is null) return new DeleteExpenseResultAppDto { Success = false, ErrorMessage = "Error.ExpenseIsNotFound" };

        await expenseManager.DeleteExpenseAsync(id).ConfigureAwait(false);
        return new DeleteExpenseResultAppDto { Success = true };
    }

    public async Task<IEnumerable<ExpenseSummaryAppDto>> GetExpenseSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await expenseManager.GetExpensesAsync(0, int.MaxValue, fromDate: fromDate, toDate: toDate).ConfigureAwait(false);
        var summaryLines = data.GroupBy(expense => expense.ExpenseType)
            .Select(group => new ExpenseSummaryAppDto
            {
                ExpenseType = (int)group.Key,
                Count = group.Count(),
                TotalAmount = group.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        return summaryLines;
    }
}
