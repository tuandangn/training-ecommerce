using MediatR;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Web.Contracts.Models.Finance;
using NamEcommerce.Web.Contracts.Queries.Models.Finance;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Finance;

public sealed class GetExpenseBudgetsHandler(IExpenseBudgetAppService budgetAppService, IExpenseAppService expenseAppService)
    : IRequestHandler<GetExpenseBudgetsQuery, ExpenseBudgetListModel>
{
    public async Task<ExpenseBudgetListModel> Handle(GetExpenseBudgetsQuery request, CancellationToken cancellationToken)
    {
        var year = DateTime.Now.Year;
        var fromDate = new DateTime(year, request.Month, 1);
        var toDate = fromDate.AddMonths(1).AddTicks(-1);

        var budgets = await budgetAppService.GetBudgetsForMonthAsync(year, request.Month).ConfigureAwait(false);
        var actuals = await expenseAppService.GetExpenseSummaryAsync(fromDate, toDate).ConfigureAwait(false);

        var budgetByType = budgets.ToDictionary(b => b.ExpenseType, b => b.Amount);
        var actualByType = actuals.ToDictionary(a => a.ExpenseType, a => (a.TotalAmount, a.Count));

        var expenseTypeValues = Enum.GetValues<ExpenseType>().OfType<int>();
        var items = Enumerable.Range(expenseTypeValues.Min(), expenseTypeValues.Max()).Select(typeInt =>
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
            Year = year,
            Month = request.Month,
            Items = items
        };
    }
}
