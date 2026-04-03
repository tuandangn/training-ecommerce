namespace NamEcommerce.Web.Contracts.Models.Orders;

[Serializable]
public sealed record OrderListItemModel
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public required decimal TotalAmount { get; init; }
    public int Status { get; init; }
}
