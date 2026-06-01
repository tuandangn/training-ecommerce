namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public sealed record RecentSalePriceAppDto(
    Guid CustomerId,
    string? CustomerName,
    decimal UnitPrice,
    string OrderCode,
    DateTime OrderDate);
