using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public record Expense : AppAggregateEntity
{
    internal Expense(string title, ExpenseType expenseType, DateTime incurredDate) : base(Guid.NewGuid())
    {
        Title = title;
        ExpenseType = expenseType;
        IncurredDate = incurredDate;

        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Title { get; internal set; }
    public string? Description { get; internal set; }
    public ExpenseType ExpenseType { get; internal set; }
    public DateTime IncurredDate { get; internal set; }

    public decimal? TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Amount { get; private set; }
    public decimal AmountExcludingTax => Amount - TaxAmount;

    public PaymentMethod? PaymentMethod { get; private set; }
    public Guid? BankAccountId { get; private set; }

    public Guid? RecordedByUserId { get; internal set; }

    public Guid? ReferenceId { get; set; }
    public string? ReferenceCode { get; set; }
    public ExpenseReferenceType ReferenceType { get; set; }

    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? ModifiedOnUtc { get; internal set; }

    internal void SetAmount(decimal amountWithoutTax, decimal? taxRate)
    {
        if (amountWithoutTax <= 0)
            throw new ExpenseAmountCannotBeNegativeException();

        if (taxRate.HasValue && taxRate < 0)
            throw new ExpenseTaxRateInvalidException();

        var taxAmount = taxRate.HasValue ? Math.Round(amountWithoutTax * taxRate.Value, 0) : 0;
        TaxRate = taxRate;
        TaxAmount = taxAmount;
        Amount = amountWithoutTax + taxAmount;
    }

    public bool IsSystemGenerated() => ExpenseType == ExpenseType.AssetDisposal || ReferenceId.HasValue;
}
