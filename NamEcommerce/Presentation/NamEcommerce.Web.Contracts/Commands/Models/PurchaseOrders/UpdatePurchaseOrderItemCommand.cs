using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed record UpdatePurchaseOrderItemCommand : IRequest<CommonActionResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid PurchaseOrderItemId { get; init; }
    public decimal Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public string? Note { get; init; }
}
