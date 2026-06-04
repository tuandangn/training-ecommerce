using MediatR;
using NamEcommerce.Web.Contracts.Models.Finance;

namespace NamEcommerce.Web.Contracts.Queries.Models.Finance;

[Serializable]
public sealed class GetExpensesQuery : IRequest<ExpenseListModel>
{
    public string? Keywords { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int? ExpenseType { get; init; }
    public int Page { get; init; } = 1;
    public string? SortBy { get; init; }
    public bool SortDesc { get; init; } = true;
}

[Serializable]
public sealed record GetExpenseByIdQuery(Guid Id) : IRequest<ExpenseEditModel?>;

[Serializable]
public sealed class GetExpenseBudgetsQuery : IRequest<ExpenseBudgetListModel>
{
    public int Year { get; init; }
    public int Month { get; init; }
}
