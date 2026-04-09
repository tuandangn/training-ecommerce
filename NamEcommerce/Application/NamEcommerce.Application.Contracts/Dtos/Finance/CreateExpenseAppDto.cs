using System.ComponentModel.DataAnnotations;

namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public class CreateExpenseAppDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên khoản chi.")]
    [StringLength(255, ErrorMessage = "Tên khoản chi không được vượt quá 255 ký tự.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập số tiền.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0.")]
    public decimal Amount { get; set; }

    public int ExpenseType { get; set; }

    public DateTime IncurredDate { get; set; }
    
    public Guid? RecordedByUserId { get; set; }

    public (bool isValid, string? errorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(Title)) return (false, "Title is required.");
        if (Amount < 0) return (false, "Amount cannot be negative.");
        return (true, null);
    }
}
