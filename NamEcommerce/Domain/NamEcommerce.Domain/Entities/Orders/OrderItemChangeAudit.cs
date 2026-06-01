using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Domain.Entities.Orders;

[Serializable]
public sealed record OrderItemChangeAudit : AppAggregateEntity
{
    internal OrderItemChangeAudit(
        Guid id,
        Guid orderId,
        Guid orderItemId,
        Guid productId,
        string productName,
        OrderItemChangeAuditAction action,
        decimal? oldQuantity,
        decimal? newQuantity,
        decimal? oldUnitPrice,
        decimal? newUnitPrice,
        Guid? changedByUserId,
        string? changedByUsername,
        DateTime createdOnUtc) : base(id)
    {
        OrderId = orderId;
        OrderItemId = orderItemId;
        ProductId = productId;
        ProductName = productName;
        Action = action;
        OldQuantity = oldQuantity;
        NewQuantity = newQuantity;
        OldUnitPrice = oldUnitPrice;
        NewUnitPrice = newUnitPrice;
        ChangedByUserId = changedByUserId;
        ChangedByUsername = changedByUsername;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid OrderId { get; init; }
    public Guid OrderItemId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; }
    public OrderItemChangeAuditAction Action { get; init; }
    public decimal? OldQuantity { get; init; }
    public decimal? NewQuantity { get; init; }
    public decimal? OldUnitPrice { get; init; }
    public decimal? NewUnitPrice { get; init; }
    public Guid? ChangedByUserId { get; init; }
    public string? ChangedByUsername { get; init; }
    public DateTime CreatedOnUtc { get; init; }

    internal static OrderItemChangeAudit Create(CreateOrderItemChangeAuditDto dto)
    {
        dto.Verify();

        return new OrderItemChangeAudit(
            Guid.NewGuid(),
            dto.OrderId,
            dto.OrderItemId,
            dto.ProductId,
            dto.ProductName,
            dto.Action,
            dto.OldQuantity,
            dto.NewQuantity,
            dto.OldUnitPrice,
            dto.NewUnitPrice,
            dto.ChangedByUserId,
            dto.ChangedByUsername,
            DateTime.UtcNow);
    }
}
