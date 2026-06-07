using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;
using PaymentMethodEnum = NamEcommerce.Domain.Shared.Enums.Orders.PaymentMethod;

namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public record Expense : AppAggregateEntity
{
    internal Expense(Guid id, string title, decimal amount, ExpenseType expenseType, DateTime incurredDate, Guid? recordedByUserId,
        decimal? taxRate = null, PaymentMethodEnum? paymentMethod = null, Guid? bankAccountId = null) : base(id)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ExpenseTitleRequiredException();
        if (amount < 0) throw new ExpenseAmountCannotBeNegativeException();

        Title = title;
        Amount = amount;
        ExpenseType = expenseType;
        IncurredDate = incurredDate;
        RecordedByUserId = recordedByUserId;
        TaxRate = taxRate;
        TaxAmount = taxRate.HasValue ? Math.Round(amount * taxRate.Value, 0) : 0;
        PaymentMethod = paymentMethod;
        BankAccountId = paymentMethod == PaymentMethodEnum.BankTransfer ? bankAccountId : null;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Title { get; private set; }
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }
    public ExpenseType ExpenseType { get; private set; }
    public DateTime IncurredDate { get; private set; }

    // PRE-4c: Thuế GTGT đầu vào trên chi phí (TK 133)
    public decimal? TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal AmountExcludingTax => Amount - TaxAmount;   // computed — không persist

    // PRE-4c: Phương thức thanh toán — để phân biệt TK111/TK112 trong sổ quỹ
    public PaymentMethodEnum? PaymentMethod { get; private set; }
    public Guid? BankAccountId { get; private set; }

    public Guid? RecordedByUserId { get; private set; }

    public Guid? SourceVendorReturnId { get; internal set; }
    public Guid? SourceCustomerReturnId { get; internal set; }
    public Guid? SourceOrderId { get; internal set; }

    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ModifiedOnUtc { get; private set; }

    internal void SetDescription(string? description)
    {
        Description = description;
    }

    public void UpdateInfo(string title, string? description, decimal amount, ExpenseType expenseType, DateTime incurredDate,
        decimal? taxRate = null, PaymentMethodEnum? paymentMethod = null, Guid? bankAccountId = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ExpenseTitleRequiredException();
        if (amount < 0) throw new ExpenseAmountCannotBeNegativeException();

        Title = title;
        Description = description;
        Amount = amount;
        ExpenseType = expenseType;
        IncurredDate = incurredDate;
        TaxRate = taxRate;
        TaxAmount = taxRate.HasValue ? Math.Round(amount * taxRate.Value, 0) : 0;
        PaymentMethod = paymentMethod;
        BankAccountId = paymentMethod == PaymentMethodEnum.BankTransfer ? bankAccountId : null;
        ModifiedOnUtc = DateTime.UtcNow;
    }
}
