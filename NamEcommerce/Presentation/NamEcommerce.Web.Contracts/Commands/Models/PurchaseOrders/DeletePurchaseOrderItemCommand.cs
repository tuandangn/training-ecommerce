using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

[Serializable]
public sealed record DeletePurchaseOrderItemCommand : ICommand<CommonActionResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public required Guid ItemId { get; init; }
}
