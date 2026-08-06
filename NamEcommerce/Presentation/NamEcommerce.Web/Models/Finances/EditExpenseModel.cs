using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Web.Models.Finances;

[Serializable]
public sealed class EditExpenseModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal AmountWithoutTax { get; set; }
    public decimal? TaxRate { get; set; }

    public ExpenseType ExpenseType { get; set; }

    public DateTime IncurredDate { get; set; }

    [ValidateNever]
    public bool IsSystemGenerated { get; set; }
}
