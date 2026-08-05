using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Domain.Shared.Dtos.Finance;

[Serializable]
public sealed record ExpenseDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; set; }
    public ExpenseType ExpenseType { get; set; }
    public DateTime IncurredDate { get; set; }

    public decimal? TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public required decimal Amount { get; init; }
    public decimal AmountWithoutTax { get; set; }

    public Guid? SourceOrderId { get; set; }
    public Guid? SourceCustomerReturnId { get; set; }
    public Guid? SourceVendorReturnId { get; set; }

    public bool IsSystemGenerated { get; set; }
}
