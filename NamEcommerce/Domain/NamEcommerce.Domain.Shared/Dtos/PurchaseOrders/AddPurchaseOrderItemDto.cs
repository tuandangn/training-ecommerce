namespace NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

[Serializable]
public sealed record AddPurchaseOrderItemDto : BasePurchaseOrderItemDto;

[Serializable]
public sealed record AddPurchaseOrderItemResultDto
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid CreatedItemId { get; init; }
}
