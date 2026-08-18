using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

[Serializable]
public sealed class UpdateExpenseCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; set; }
    public required decimal AmountWithoutTax { get; init; }
    public decimal? TaxRate { get; set; }
    public required int ExpenseType { get; init; }
    public required DateTime IncurredDate { get; init; }
}
