using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public class CreateExpenseAppDto
{
    [Required(ErrorMessage = "Error.ExpenseTitleRequired")]
    [StringLength(255, ErrorMessage = "Error.ExpenseTitleTooLong")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Error.ExpenseAmountRequired")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Error.ExpenseAmountMustBePositive")]
    public decimal Amount { get; set; }

    public int ExpenseType { get; set; }

    public DateTime IncurredDate { get; set; }
    
    public Guid? RecordedByUserId { get; set; }

    public (bool isValid, string? errorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(Title)) return (false, "Error.ExpenseTitleRequired");
        if (Amount < 0) return (false, "Error.ExpenseAmountMustBePositive");
        return (true, null);
    }
}
