namespace NamEcommerce.Application.Contracts.Dtos.Finance;

[Serializable]
public sealed class ExpenseSummaryAppDto
{
    public int ExpenseType { get; set; }
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

[Serializable]
public sealed class ExpenseAppDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal AmountWithoutTax { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal Amount { get; set; }
    public int ExpenseType { get; set; }
    public DateTime IncurredDateUtc { get; set; }

    public Guid? ReferenceId { get; set; }
    public string? ReferenceCode { get; set; }
    public int ReferenceType { get; set; }

    public bool IsSystemGenerated { get; init; }
}
