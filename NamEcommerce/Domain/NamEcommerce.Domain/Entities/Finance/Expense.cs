using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public record Expense : AppAggregateEntity
{
    internal Expense(Guid id, string title, decimal amount, ExpenseType expenseType, DateTime incurredDate, Guid? recordedByUserId) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền không được âm");

        Title = title;
        Amount = amount;
        ExpenseType = expenseType;
        IncurredDate = incurredDate;
        RecordedByUserId = recordedByUserId;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Title { get; private set; }
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }
    public ExpenseType ExpenseType { get; private set; }
    public DateTime IncurredDate { get; private set; }
    
    public Guid? RecordedByUserId { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ModifiedOnUtc { get; private set; }

    public void UpdateInfo(string title, string? description, decimal amount, ExpenseType expenseType, DateTime incurredDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Số tiền không được âm");

        Title = title;
        Description = description;
        Amount = amount;
        ExpenseType = expenseType;
        IncurredDate = incurredDate;
        ModifiedOnUtc = DateTime.UtcNow;
    }
}
