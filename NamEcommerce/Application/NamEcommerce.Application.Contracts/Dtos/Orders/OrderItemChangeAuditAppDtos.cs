namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public sealed record OrderItemChangeAuditAppDto(Guid Id)
{
    public required Guid OrderId { get; init; }
    public required Guid OrderItemId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required int Action { get; init; }
    public decimal? OldQuantity { get; init; }
    public decimal? NewQuantity { get; init; }
    public decimal? OldUnitPrice { get; init; }
    public decimal? NewUnitPrice { get; init; }
    public Guid? ChangedByUserId { get; init; }
    public string? ChangedByUsername { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}
