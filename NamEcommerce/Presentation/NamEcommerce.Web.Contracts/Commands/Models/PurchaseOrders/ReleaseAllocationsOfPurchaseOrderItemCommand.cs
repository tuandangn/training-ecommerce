using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

public sealed class ReleaseAllocationsOfPurchaseOrderItemCommand : IRequest<CommonActionResultModel>
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid PurchaseOrderItemId { get; init; }
}
