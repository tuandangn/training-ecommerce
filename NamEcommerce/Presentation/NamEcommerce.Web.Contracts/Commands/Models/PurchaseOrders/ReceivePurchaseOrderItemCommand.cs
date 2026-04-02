using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class ReceivePurchaseOrderItemCommand : IRequest<ReceivePurchaseOrderItemResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid? WarehouseId { get; set; }
    public required decimal ReceivedQuantity { get; set; }
    public Guid? ReceivedByUserId { get; set; }
}
