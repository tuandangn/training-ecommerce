using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Domain.Shared.Dtos.Finance;

public class UpsertExpenseBudgetDto
{
    public ExpenseType ExpenseType { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
}
