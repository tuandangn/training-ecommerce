using MediatR;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

public sealed class SplitPurchaseOrderCommand : IRequest<CreatePurchaseOrderResultModel>
{
    public required Guid PurchaseOrderId { get; init; }
    public required IList<SplitItemCommand> Items { get; init; }

    public sealed class SplitItemCommand
    {
        public Guid ItemId { get; init; }
        public decimal Quantity { get; init; }
    }
}
