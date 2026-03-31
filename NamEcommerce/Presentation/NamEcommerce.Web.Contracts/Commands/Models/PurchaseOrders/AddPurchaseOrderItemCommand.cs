using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class AddPurchaseOrderItemCommand : IRequest<AddPurchaseOrderItemResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid ProductId { get; init; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Note { get; init; }
}
