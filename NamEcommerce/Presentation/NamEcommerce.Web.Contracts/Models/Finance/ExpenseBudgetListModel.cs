namespace NamEcommerce.Web.Contracts.Models.Finance;

[Serializable]
public sealed class ExpenseBudgetListModel
{
    public int Year { get; init; }
    public int Month { get; init; }
    public IReadOnlyCollection<BudgetItem> Items { get; init; } = [];

    public sealed class BudgetItem
    {
        public int ExpenseType { get; init; }
        public decimal BudgetAmount { get; init; }
        public decimal ActualAmount { get; init; }
        public int Count { get; init; }
    }
}
