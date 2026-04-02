using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed class SubmitsPurchaseOrderCommand : IRequest<CommonResultModel>
{
    public Guid PurchaseOrderId { get; set; }
}
