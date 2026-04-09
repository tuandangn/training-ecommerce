namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public class ExpenseAppDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int ExpenseType { get; set; }
    public DateTime IncurredDate { get; set; }
}
