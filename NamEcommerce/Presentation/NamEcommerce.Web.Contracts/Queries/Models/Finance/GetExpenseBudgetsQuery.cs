using MediatR;
using NamEcommerce.Web.Contracts.Models.Finance;

namespace NamEcommerce.Web.Contracts.Queries.Models.Finance;

[Serializable]
public sealed record GetExpenseBudgetsQuery : IRequest<ExpenseBudgetListModel>
{
    public int Month { get; init; }
}
