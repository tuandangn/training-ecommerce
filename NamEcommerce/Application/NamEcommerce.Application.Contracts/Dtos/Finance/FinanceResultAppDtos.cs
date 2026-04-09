namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public class CreateExpenseResultAppDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid CreatedId { get; set; }
}

public class UpdateExpenseResultAppDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid UpdatedId { get; set; }
}

public class DeleteExpenseResultAppDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
