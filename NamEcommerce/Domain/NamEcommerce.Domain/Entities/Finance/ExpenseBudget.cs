using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public sealed record ExpenseBudget : AppAggregateEntity
{
    internal ExpenseBudget(Guid id, ExpenseType expenseType, int year, int month, decimal amount) : base(id)
    {
        ExpenseType = expenseType;
        Year = year;
        Month = month;
        Amount = amount;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public ExpenseType ExpenseType { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ModifiedOnUtc { get; private set; }

    internal void UpdateAmount(decimal amount)
    {
        Amount = amount;
        ModifiedOnUtc = DateTime.UtcNow;
    }
}
