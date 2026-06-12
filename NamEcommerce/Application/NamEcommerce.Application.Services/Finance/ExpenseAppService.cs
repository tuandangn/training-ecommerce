using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Dtos.Finance;

namespace NamEcommerce.Application.Services.Finance;

public class ExpenseAppService : IExpenseAppService
{
    private readonly IExpenseManager _expenseManager;
    private readonly IEntityDataReader<Expense> _expenseDataReader;

    public ExpenseAppService(IExpenseManager expenseManager, IEntityDataReader<Expense> expenseDataReader)
    {
        _expenseManager = expenseManager;
        _expenseDataReader = expenseDataReader;
    }

    public async Task<IPagedDataAppDto<ExpenseAppDto>> GetExpensesAsync(string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null, int? expenseType = null, int pageIndex = 0, int pageSize = int.MaxValue, string? sortBy = null, bool sortDesc = true)
    {
        var query = _expenseDataReader.DataSource;
        if (!string.IsNullOrWhiteSpace(keywords))
            query = query.Where(x => x.Title.Contains(keywords) || (x.Description != null && x.Description.Contains(keywords)));

        if (fromDate.HasValue)
            query = query.Where(x => x.IncurredDate >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(x => x.IncurredDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        if (expenseType.HasValue)
        {
            var typeEnum = (NamEcommerce.Domain.Shared.Enums.Finance.ExpenseType)expenseType.Value;
            query = query.Where(x => x.ExpenseType == typeEnum);
        }

        var totalCount = query.Count();

        IOrderedQueryable<Expense> sorted = sortBy switch
        {
            "amount" => sortDesc ? query.OrderByDescending(x => x.Amount) : query.OrderBy(x => x.Amount),
            "title"  => sortDesc ? query.OrderByDescending(x => x.Title)  : query.OrderBy(x => x.Title),
            "type"   => sortDesc ? query.OrderByDescending(x => x.ExpenseType) : query.OrderBy(x => x.ExpenseType),
            _        => sortDesc ? query.OrderByDescending(x => x.IncurredDate) : query.OrderBy(x => x.IncurredDate)
        };

        var items = sorted
            .Skip(pageIndex * pageSize).Take(pageSize)
            .Select(x => new ExpenseAppDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Amount = x.Amount,
                ExpenseType = (int)x.ExpenseType,
                IncurredDate = x.IncurredDate,
                SourceOrderId = x.SourceOrderId,
                SourceCustomerReturnId = x.SourceCustomerReturnId,
                SourceVendorReturnId = x.SourceVendorReturnId
            })
            .ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, totalCount);
    }

    public Task<IList<ExpenseAppDto>> GetExpensesByOrderIdAsync(Guid orderId)
    {
        var items = _expenseDataReader.DataSource
            .Where(x => x.SourceOrderId == orderId)
            .OrderByDescending(x => x.IncurredDate)
            .Select(x => new ExpenseAppDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Amount = x.Amount,
                ExpenseType = (int)x.ExpenseType,
                IncurredDate = x.IncurredDate,
                SourceOrderId = x.SourceOrderId,
                SourceCustomerReturnId = x.SourceCustomerReturnId,
                SourceVendorReturnId = x.SourceVendorReturnId
            })
            .ToList();

        return Task.FromResult<IList<ExpenseAppDto>>(items);
    }

    public async Task<ExpenseAppDto?> GetExpenseByIdAsync(Guid id)
    {
        var x = await _expenseDataReader.GetByIdAsync(id, default);
        if (x is null) return null;

        return MapExpense(x);
    }

    public async Task<CreateExpenseResultAppDto> CreateExpenseAsync(CreateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var result = await _expenseManager.CreateExpenseAsync(new CreateExpenseDto
        {
            Title = dto.Title,
            Description = dto.Description,
            Amount = dto.Amount,
            ExpenseType = (NamEcommerce.Domain.Shared.Enums.Finance.ExpenseType)dto.ExpenseType,
            IncurredDate = dto.IncurredDate,
            RecordedByUserId = dto.RecordedByUserId,
            SourceOrderId = dto.SourceOrderId
        });

        return new CreateExpenseResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<UpdateExpenseResultAppDto> UpdateExpenseAsync(UpdateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new UpdateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var expense = await _expenseDataReader.GetByIdAsync(dto.Id, default);
        if (expense is null) return new UpdateExpenseResultAppDto { Success = false, ErrorMessage = "Error.ExpenseIsNotFound" };

        await _expenseManager.UpdateExpenseAsync(new UpdateExpenseDto(dto.Id)
        {
            Title = dto.Title,
            Description = dto.Description,
            Amount = dto.Amount,
            ExpenseType = (NamEcommerce.Domain.Shared.Enums.Finance.ExpenseType)dto.ExpenseType,
            IncurredDate = dto.IncurredDate
        });

        return new UpdateExpenseResultAppDto { Success = true, UpdatedId = dto.Id };
    }

    public async Task<DeleteExpenseResultAppDto> DeleteExpenseAsync(Guid id)
    {
        var expense = await _expenseDataReader.GetByIdAsync(id, default);
        if (expense is null) return new DeleteExpenseResultAppDto { Success = false, ErrorMessage = "Error.ExpenseIsNotFound" };

        await _expenseManager.DeleteExpenseAsync(id);
        return new DeleteExpenseResultAppDto { Success = true };
    }

    public Task<IReadOnlyCollection<ExpenseSummaryAppDto>> GetExpenseSummaryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _expenseDataReader.DataSource;
        if (fromDate.HasValue)
            query = query.Where(x => x.IncurredDate >= fromDate.Value.Date);
        if (toDate.HasValue)
            query = query.Where(x => x.IncurredDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        var result = query
            .GroupBy(x => x.ExpenseType)
            .Select(g => new ExpenseSummaryAppDto
            {
                ExpenseType = (int)g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.TotalAmount)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<ExpenseSummaryAppDto>>(result);
    }

    private static ExpenseAppDto MapExpense(Expense expense)
        => new()
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            Amount = expense.Amount,
            ExpenseType = (int)expense.ExpenseType,
            IncurredDate = expense.IncurredDate,
            SourceOrderId = expense.SourceOrderId,
            SourceCustomerReturnId = expense.SourceCustomerReturnId,
            SourceVendorReturnId = expense.SourceVendorReturnId
        };
}
