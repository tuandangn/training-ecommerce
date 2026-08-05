namespace NamEcommerce.Application.Contracts.Dtos.Finance;

[Serializable]
public sealed record UpdateExpenseAppDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal AmountWithoutTax { get; set; }
    public decimal? TaxRate { get; set; }

    public int ExpenseType { get; set; }

    public DateTime IncurredDateUtc { get; set; }

    public (bool isValid, string? errorMessage) Validate()
    {
        if (IncurredDateUtc > DateTime.UtcNow)
            return (false, "Error.ExpenseDataIsInvalid");

        if (string.IsNullOrWhiteSpace(Title))
            return (false, "Error.ExpenseTitleRequired");
        else if (Title.Length > 255)
            return (false, "Error.ExpenseTitleTooLong");

        if (AmountWithoutTax <= 0)
            return (false, "Error.ExpenseAmountMustBePositive");

        if (TaxRate.HasValue && TaxRate < 0)
            return (false, "Error.ExpenseTaxRateInvalid");

        return (true, null);
    }

}
