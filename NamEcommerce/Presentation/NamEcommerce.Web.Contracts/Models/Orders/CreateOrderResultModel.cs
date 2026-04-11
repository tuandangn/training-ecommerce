namespace NamEcommerce.Web.Contracts.Models.Orders;

[Serializable]
public sealed record CreateOrderResultModel
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; init; }
    public string? ErrorMessage { get; init; }
}
