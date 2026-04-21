using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class CancelPurchaseOrderCommand : IRequest<CommonActionResultModel>
{
    public Guid PurchaseOrderId { get; set; }
}
