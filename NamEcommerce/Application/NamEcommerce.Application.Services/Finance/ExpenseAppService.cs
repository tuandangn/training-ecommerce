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

    public async Task<IPagedDataAppDto<ExpenseAppDto>> GetExpensesAsync(string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null, int? expenseType = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _expenseDataReader.DataSource;
        if (!string.IsNullOrWhiteSpace(keywords))
        {
            query = query.Where(x => x.Title.Contains(keywords) || (x.Description != null && x.Description.Contains(keywords)));
        }
        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date.ToUniversalTime();
            query = query.Where(x => x.IncurredDate >= from);
        }
        if (toDate.HasValue)
        {
            var to = toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(x => x.IncurredDate <= to);
        }
        if (expenseType.HasValue)
        {
            var typeEnum = (NamEcommerce.Domain.Shared.Enums.Finance.ExpenseType)expenseType.Value;
            query = query.Where(x => x.ExpenseType == typeEnum);
        }

        var totalCount = query.Count();
        var items = query.OrderByDescending(x => x.IncurredDate)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .Select(x => new ExpenseAppDto
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Amount = x.Amount,
                ExpenseType = (int)x.ExpenseType,
                IncurredDate = x.IncurredDate
            }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, totalCount);
    }

    public async Task<ExpenseAppDto?> GetExpenseByIdAsync(Guid id)
    {
        var x = await _expenseDataReader.GetByIdAsync(id);
        if (x is null) return null;

        return new ExpenseAppDto
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            Amount = x.Amount,
            ExpenseType = (int)x.ExpenseType,
            IncurredDate = x.IncurredDate
        };
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
            RecordedByUserId = dto.RecordedByUserId
        });

        return new CreateExpenseResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<UpdateExpenseResultAppDto> UpdateExpenseAsync(UpdateExpenseAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new UpdateExpenseResultAppDto { Success = false, ErrorMessage = errorMessage };

        var expense = await _expenseDataReader.GetByIdAsync(dto.Id);
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
        var expense = await _expenseDataReader.GetByIdAsync(id);
        if (expense is null) return new DeleteExpenseResultAppDto { Success = false, ErrorMessage = "Error.ExpenseIsNotFound" };

        await _expenseManager.DeleteExpenseAsync(id);
        return new DeleteExpenseResultAppDto { Success = true };
    }
}
