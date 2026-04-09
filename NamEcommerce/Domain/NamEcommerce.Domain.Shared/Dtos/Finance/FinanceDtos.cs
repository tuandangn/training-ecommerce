using NamEcommerce.Domain.Shared.Enums.Finance;

namespace NamEcommerce.Domain.Shared.Dtos.Finance;

public class CreateExpenseDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public ExpenseType ExpenseType { get; set; }
    public DateTime IncurredDate { get; set; }
    public Guid? RecordedByUserId { get; set; }
}

public class UpdateExpenseDto : CreateExpenseDto
{
    public Guid Id { get; set; }
    public UpdateExpenseDto(Guid id) => Id = id;
}

public class CreateExpenseResultDto
{
    public Guid CreatedId { get; set; }
}
