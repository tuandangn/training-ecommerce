namespace NamEcommerce.Web.Contracts.Models.Finance;

[Serializable]
public sealed class ExpenseModel
{
    public required Guid Id { get; set; }
    public required string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public required decimal AmountWithoutTax { get; set; }
    public decimal? TaxRate { get; set; }

    public int ExpenseType { get; set; }

    public DateTime IncurredDate { get; set; }

    public bool IsSystemGenerated { get; set; }
}
