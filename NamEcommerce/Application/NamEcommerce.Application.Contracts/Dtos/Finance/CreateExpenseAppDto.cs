namespace NamEcommerce.Application.Contracts.Dtos.Finance;

[Serializable]
public sealed record CreateExpenseAppDto
{
    public DateTime IncurredDateUtc { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ExpenseType { get; set; }
    public Guid? RecordedByUserId { get; set; }

    public decimal AmountWithoutTax { get; set; }
    public decimal? TaxRate { get; set; }

    public (bool isValid, string? errorMessage) Validate()
    {
        if (IncurredDateUtc > DateTime.UtcNow)
            return (false, "Error.ExpenseIncurredDateCannotBeInFuture");

        if (string.IsNullOrEmpty(Title))
            return (false, "Error.ExpenseTitleRequired");

        if (AmountWithoutTax <= 0)
            return (false, "Error.ExpenseAmountMustBePositive");

        if (TaxRate.HasValue && TaxRate < 0)
            return (false, "Error.ExpenseTaxRateInvalid");

        return (true, null);
    }
}
