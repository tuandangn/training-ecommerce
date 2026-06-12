namespace NamEcommerce.Web.Contracts.Models.Returns;

[Serializable]
public sealed record CreateCustomerReturnResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
}
