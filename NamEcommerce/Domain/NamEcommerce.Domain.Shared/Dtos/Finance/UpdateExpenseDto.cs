using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Shared.Dtos.Finance;

[Serializable]
public sealed class UpdateExpenseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal AmountWithoutTax { get; set; }
    public decimal? TaxRate { get; set; }
    public DateTime IncurredDateUtc { get; set; }
    public ExpenseType ExpenseType { get; set; }

    public void Verify()
    {
        if (IncurredDateUtc > DateTime.UtcNow)
            throw new ExpenseDataIsInvalidException("Error.ExpenseIncurredDateCannotBeInFuture");

        if (string.IsNullOrWhiteSpace(Title))
            throw new ExpenseDataIsInvalidException("Error.ExpenseTitleRequired");
        else if (Title.Length > 255)
            throw new ExpenseDataIsInvalidException("Error.ExpenseTitleTooLong");

        if (AmountWithoutTax <= 0)
            throw new ExpenseDataIsInvalidException("Error.ExpenseAmountMustBePositive");

        if (TaxRate.HasValue && TaxRate < 0)
            throw new ExpenseTaxRateInvalidException();
    }
}
