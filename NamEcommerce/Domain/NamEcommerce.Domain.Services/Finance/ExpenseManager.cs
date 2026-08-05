using Microsoft.EntityFrameworkCore;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Finances;

namespace NamEcommerce.Domain.Services.Finance;

public class ExpenseManager(IRepository<Expense> expenseRepository, IEntityDataReader<Expense> expenseDataReader) : IExpenseManager
{
    public async Task<CreateExpenseResultDto> CreateExpenseAsync(CreateExpenseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        if (dto.SourceVendorReturnId.HasValue)
        {
            var existing = await expenseDataReader.DataSource
                .FirstOrDefaultAsync(e => e.SourceVendorReturnId == dto.SourceVendorReturnId.Value).ConfigureAwait(false);
            if (existing is not null)
                return new CreateExpenseResultDto { CreatedId = existing.Id };
        }
        if (dto.SourceCustomerReturnId.HasValue)
        {
            var existing = await expenseDataReader.DataSource
                .FirstOrDefaultAsync(e => e.SourceCustomerReturnId == dto.SourceCustomerReturnId.Value).ConfigureAwait(false);
            if (existing is not null)
                return new CreateExpenseResultDto { CreatedId = existing.Id };
        }

        var expense = new Expense(dto.Title, dto.ExpenseType, dto.IncurredDateUtc)
        {
            RecordedByUserId = dto.RecordedByUserId,
            SourceVendorReturnId = dto.SourceVendorReturnId,
            SourceCustomerReturnId = dto.SourceCustomerReturnId,
            SourceOrderId = dto.SourceOrderId,
            Description = dto.Description,
        };
        expense.SetAmount(dto.AmountWithoutTax, dto.TaxRate);

        await expenseRepository.InsertAsync(expense);
        return new CreateExpenseResultDto { CreatedId = expense.Id };
    }

    public async Task UpdateExpenseAsync(UpdateExpenseDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var expense = await expenseRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (expense is null)
            throw new ArgumentException($"Expense with ID {dto.Id} not found.");

        expense.Title = dto.Title;
        expense.Description = dto.Description;
        expense.IncurredDate = dto.IncurredDateUtc;
        expense.ExpenseType = dto.ExpenseType;
        expense.SetAmount(dto.AmountWithoutTax, dto.TaxRate);

        await expenseRepository.UpdateAsync(expense);
    }

    public async Task DeleteExpenseAsync(Guid id)
    {
        var expense = await expenseRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (expense is not null)
            await expenseRepository.DeleteAsync(expense);
    }

    public async Task<IPagedDataDto<ExpenseDto>> GetExpensesAsync(
        int pageIndex, int pageSize, IList<Guid>? orderIds = null,
        string? keywords = null, DateTime? fromDate = null, DateTime? toDate = null,
        ExpenseType? expenseType = null, ExpenseSortByEnum? sortBy = null)
    {
        var expenseSpec = new CompositeSpecification<Expense>();

        if (orderIds != null && orderIds.Count > 0)
            expenseSpec.And(new ExpensesOfOrdersSpec(orderIds));

        if (!string.IsNullOrEmpty(keywords))
            expenseSpec.And(new ExpenseKeywordSearchSpec(keywords));

        expenseSpec.And(new DateRangeExpenseSpec((fromDate, toDate)));

        if (expenseType.HasValue)
            expenseSpec.And(new HaveTypesExpenseSpec([expenseType.Value]));

        int? totalCount = pageIndex == 0 && pageSize == int.MaxValue 
            ? null
            : await expenseDataReader.CountAsync(expenseSpec).ConfigureAwait(false);

        if (totalCount.HasValue && totalCount == 0)
            return PagedDataDto.Create(new List<ExpenseDto>(), pageIndex, pageSize, 0);

        switch (sortBy)
        {
            case ExpenseSortByEnum.Amount:
                expenseSpec.ApplyOrderBy(expense => expense.Amount);
                break;
            case ExpenseSortByEnum.AmountDesc:
                expenseSpec.ApplyOrderByDescending(expense => expense.Amount);
                break;
            case ExpenseSortByEnum.Title:
                expenseSpec.ApplyOrderBy(expense => expense.Title);
                break;
            case ExpenseSortByEnum.TitleDesc:
                expenseSpec.ApplyOrderByDescending(expense => expense.Title);
                break;
            case ExpenseSortByEnum.ExpenseType:
                expenseSpec.ApplyOrderBy(expense => expense.ExpenseType);
                break;
            case ExpenseSortByEnum.IncurredDate:
                expenseSpec.ApplyOrderBy(expense => expense.IncurredDate);
                break;
            case ExpenseSortByEnum.IncurredDateDesc:
            default:
                expenseSpec.ApplyOrderByDescending(expense => expense.IncurredDate);
                break;
        }
        var pagedData = await expenseDataReader.GetPagedListAsync(expenseSpec, pageIndex, pageSize).ConfigureAwait(false);

        return PagedDataDto.Create(pagedData.Select(expense => new ExpenseDto
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            Amount = expense.Amount,
            ExpenseType = expense.ExpenseType,
            IncurredDate = expense.IncurredDate,
            SourceOrderId = expense.SourceOrderId,
            SourceCustomerReturnId = expense.SourceCustomerReturnId,
            SourceVendorReturnId = expense.SourceVendorReturnId,
            AmountWithoutTax = expense.AmountExcludingTax,
            TaxAmount = expense.TaxAmount,
            TaxRate = expense.TaxRate,
            IsSystemGenerated = expense.IsSystemGenerated()
        }).ToList(), pageIndex, pageSize, totalCount);
    }

    public async Task<ExpenseDto?> GetExpenseByIdAsync(Guid id)
    {
        var expense = await expenseRepository.GetByIdAsync(id);

        if (expense is null)
            return null;

        return new ExpenseDto
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            Amount = expense.Amount,
            ExpenseType = expense.ExpenseType,
            IncurredDate = expense.IncurredDate,
            SourceOrderId = expense.SourceOrderId,
            SourceCustomerReturnId = expense.SourceCustomerReturnId,
            SourceVendorReturnId = expense.SourceVendorReturnId,
            AmountWithoutTax = expense.AmountExcludingTax,
            TaxAmount = expense.TaxAmount,
            TaxRate = expense.TaxRate,
            IsSystemGenerated = expense.IsSystemGenerated()
        };
    }
}
