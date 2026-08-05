using MediatR;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.Finance;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Finance;

public sealed class GetExpensesHandler(IExpenseAppService expenseAppService, IExpenseBudgetAppService budgetAppService)
    : IRequestHandler<GetExpensesQuery, ExpenseListModel>
{
    private const int PageSize = 20;

    public async Task<ExpenseListModel> Handle(GetExpensesQuery request, CancellationToken cancellationToken)
    {
        var pageIndex = Math.Max(0, request.Page - 1);

        var now = DateTime.Now;
        var summaryFrom = DateTimeHelper.ToUniversalTime(new DateTime(now.Year, now.Month, 1));
        var summaryTo = DateTimeHelper.ToUniversalTime(summaryFrom.AddMonths(1).AddTicks(-1));

        var paged = await expenseAppService.GetExpensesAsync(
            pageIndex: pageIndex,
            pageSize: PageSize,
            keywords: request.Keywords,
            fromDate: request.FromDate.HasValue ? DateTimeHelper.ToUniversalTime(request.FromDate) : null,
            toDate: request.ToDate.HasValue ? DateTimeHelper.ToUniversalTime(request.ToDate) : null,
            expenseType: request.ExpenseType,
            sortBy: request.SortBy,
            sortDesc: request.SortDesc).ConfigureAwait(false);

        var summaryResult = await expenseAppService.GetExpenseSummaryAsync(summaryFrom, summaryTo).ConfigureAwait(false);
        var budgetsResult = await budgetAppService.GetBudgetsForMonthAsync(now.Year, now.Month).ConfigureAwait(false);

        var actuals = summaryResult.ToDictionary(a => a.ExpenseType, a => (a.TotalAmount, a.Count));
        var budgets = budgetsResult.ToDictionary(b => b.ExpenseType, b => b.Amount);

        var summary = Enumerable.Range(1, 5).Select(typeInt =>
        {
            actuals.TryGetValue(typeInt, out var actual);
            return new ExpenseListModel.SummaryItem
            {
                ExpenseType = typeInt,
                Count = actual.Count,
                TotalAmount = actual.TotalAmount,
                BudgetAmount = budgets.TryGetValue(typeInt, out var budget) ? budget : 0
            };
        }).ToList();

        var items = paged.Select(x => new ExpenseListModel.ExpenseItemModel
        {
            Id = x.Id,
            Title = x.Title,
            Description = x.Description,
            Amount = x.Amount,
            ExpenseType = x.ExpenseType,
            IncurredDate = DateTimeHelper.ToLocalTime(x.IncurredDateUtc),
            SourceOrderId = x.SourceOrderId,
            SourceCustomerReturnId = x.SourceCustomerReturnId,
            SourceVendorReturnId = x.SourceVendorReturnId,
            IsSystemGenerated = x.ExpenseType == 6 || x.SourceOrderId.HasValue
        }).ToList();

        return new ExpenseListModel
        {
            Keywords = request.Keywords,
            FromDate = request.FromDate?.ToString("yyyy-MM-dd"),
            ToDate = request.ToDate?.ToString("yyyy-MM-dd"),
            ExpenseType = request.ExpenseType,
            SortBy = request.SortBy,
            SortDesc = request.SortDesc,
            Data = PagedDataModel.Create(items, pageIndex, PageSize, paged.Pagination.TotalCount),
            PageTotal = paged.Sum(x => x.Amount),
            Summary = summary
        };
    }
}

public sealed class GetExpenseByIdHandler(IExpenseAppService expenseAppService)
    : IRequestHandler<GetExpenseByIdQuery, ExpenseModel?>
{
    public async Task<ExpenseModel?> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        var expense = await expenseAppService.GetExpenseByIdAsync(request.Id).ConfigureAwait(false);
        if (expense is null) return null;

        return new ExpenseModel
        {
            Id = expense.Id,
            Title = expense.Title,
            Description = expense.Description,
            AmountWithoutTax = expense.AmountWithoutTax,
            TaxRate = expense.TaxRate,
            ExpenseType = expense.ExpenseType,
            IncurredDate = DateTimeHelper.ToLocalTime(expense.IncurredDateUtc),
            IsSystemGenerated = expense.ExpenseType == 6 || expense.SourceOrderId.HasValue
        };
    }
}

public sealed class GetExpenseBudgetsHandler(
    IExpenseBudgetAppService budgetAppService,
    IExpenseAppService expenseAppService)
    : IRequestHandler<GetExpenseBudgetsQuery, ExpenseBudgetListModel>
{
    public async Task<ExpenseBudgetListModel> Handle(GetExpenseBudgetsQuery request, CancellationToken cancellationToken)
    {
        var fromDate = new DateTime(request.Year, request.Month, 1);
        var toDate = fromDate.AddMonths(1).AddTicks(-1);

        var budgets = await budgetAppService.GetBudgetsForMonthAsync(request.Year, request.Month).ConfigureAwait(false);
        var actuals = await expenseAppService.GetExpenseSummaryAsync(fromDate, toDate).ConfigureAwait(false);

        var budgetByType = budgets.ToDictionary(b => b.ExpenseType, b => b.Amount);
        var actualByType = actuals.ToDictionary(a => a.ExpenseType, a => (a.TotalAmount, a.Count));

        var items = Enumerable.Range(1, 5).Select(typeInt =>
        {
            actualByType.TryGetValue(typeInt, out var actual);
            return new ExpenseBudgetListModel.BudgetItem
            {
                ExpenseType = typeInt,
                BudgetAmount = budgetByType.TryGetValue(typeInt, out var budget) ? budget : 0,
                ActualAmount = actual.TotalAmount,
                Count = actual.Count
            };
        }).ToList();

        return new ExpenseBudgetListModel
        {
            Year = request.Year,
            Month = request.Month,
            Items = items
        };
    }
}
