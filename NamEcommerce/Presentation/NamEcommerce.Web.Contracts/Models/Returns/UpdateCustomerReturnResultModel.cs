namespace NamEcommerce.Web.Contracts.Models.Returns;

[Serializable]
public sealed record UpdateCustomerReturnResultModel : ICommandResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
