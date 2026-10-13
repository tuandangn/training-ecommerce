using NamEcommerce.Web.Contracts.Models.Finance;

namespace NamEcommerce.Web.Contracts.Queries.Models.Finance;

[Serializable]
public sealed record GetExpensesQuery : IRequest<ExpenseListModel>
{
    public string? Keywords { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int? ExpenseType { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; set; }
    public string? SortBy { get; init; }
    public bool SortDesc { get; init; } = true;
}
