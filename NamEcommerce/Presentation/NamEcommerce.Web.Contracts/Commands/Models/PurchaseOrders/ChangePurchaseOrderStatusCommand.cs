using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class ChangePurchaseOrderStatusCommand : IRequest<ChangePurchaseOrderStatusResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public required int Status { get; init; }
}
