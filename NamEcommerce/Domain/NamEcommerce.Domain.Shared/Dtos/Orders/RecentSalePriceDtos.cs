namespace NamEcommerce.Domain.Shared.Dtos.Orders;

[Serializable]
public sealed record RecentSalePriceDto(
    Guid CustomerId,
    string? CustomerName,
    decimal UnitPrice,
    string OrderCode,
    DateTime OrderDateUtc);
