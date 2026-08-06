using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Shared.Dtos.Finance;

[Serializable]
public sealed class CreateExpenseDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ExpenseType ExpenseType { get; set; }
    public DateTime IncurredDateUtc { get; set; }
    public Guid? RecordedByUserId { get; set; }

    public decimal AmountWithoutTax { get; set; }
    public decimal? TaxRate { get; set; }

    public Guid? SourceVendorReturnId { get; set; }
    public Guid? SourceCustomerReturnId { get; set; }
    public Guid? OrderId { get; set; }

    public void Verify()
    {
        if (IncurredDateUtc > DateTime.UtcNow)
            throw new ExpenseDataIsInvalidException("Error.ExpenseIncurredDateCannotBeInFuture");

        if (string.IsNullOrWhiteSpace(Title))
            throw new ExpenseDataIsInvalidException("Error.ExpenseTitleRequired");

        if (AmountWithoutTax <= 0)
            throw new ExpenseDataIsInvalidException("Error.ExpenseAmountMustBePositive");

        if (TaxRate.HasValue && TaxRate < 0)
            throw new ExpenseTaxRateInvalidException();
    }
}
