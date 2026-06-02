namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public class ExpenseSummaryAppDto
{
    public int ExpenseType { get; set; }
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class ExpenseAppDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int ExpenseType { get; set; }
    public DateTime IncurredDate { get; set; }
    public Guid? SourceOrderId { get; set; }
    public Guid? SourceCustomerReturnId { get; set; }
    public Guid? SourceVendorReturnId { get; set; }
}
