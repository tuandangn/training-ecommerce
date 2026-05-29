using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

public sealed class AllocatePoItemForOrderItemCommand : IRequest<CommonActionResultModel>
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid PurchaseOrderItemId { get; init; }
    public required Guid OrderId { get; set; }
    public required Guid OrderItemId { get; init; }
    public required decimal Quantity { get; init; }
    public string? DirectShipAddress { get; init; }
    public string? DirectShipContactName { get; init; }
    public string? DirectShipContactPhone { get; init; }
}
