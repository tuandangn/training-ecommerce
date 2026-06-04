namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public class ExpenseBudgetAppDto
{
    public int ExpenseType { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
}

public class UpsertExpenseBudgetAppDto
{
    public int ExpenseType { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }

    public (bool isValid, string? errorMessage) Validate()
    {
        if (ExpenseType is < 1 or > 5) return (false, "Error.ExpenseTypeInvalid");
        if (Year is < 2020 or > 2100) return (false, "Error.BudgetYearInvalid");
        if (Month is < 1 or > 12) return (false, "Error.BudgetMonthInvalid");
        if (Amount < 0) return (false, "Error.BudgetAmountMustBeNonNegative");
        return (true, null);
    }
}
